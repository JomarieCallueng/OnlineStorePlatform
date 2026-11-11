using System.Text.Json;
using Inventory.API.Data;
using Inventory.API.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Store.Shared.Common.Data;
using Store.Shared.Common.Messaging;
using Store.Shared.Events;

namespace Inventory.API.Messaging;

/// <summary>
/// Reserves stock when an order is placed and releases it on compensation.
/// Idempotent: a redelivered message is not applied twice, and its outcome is re-published
/// so the saga still advances if an earlier publish was lost.
/// </summary>
public sealed class InventoryConsumer : RabbitMqConsumer
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    protected override string QueueName => EventBus.Queues.InventoryReserve;
    protected override string[] RoutingKeys => new[] { nameof(OrderPlaced), nameof(StockReleaseRequested) };

    public InventoryConsumer(RabbitMqConnection conn, IServiceScopeFactory scopes,
        IOptions<RabbitMqOptions> opts, ILogger<InventoryConsumer> log)
        : base(conn, scopes, opts, log) { }

    protected override async Task HandleAsync(IServiceProvider sp, string eventName, string json,
        string? correlationId, CancellationToken ct)
    {
        var db = sp.GetRequiredService<InventoryDbContext>();
        var publisher = sp.GetRequiredService<IEventPublisher>();

        switch (eventName)
        {
            case nameof(OrderPlaced):
                await OnOrderPlaced(db, publisher, JsonSerializer.Deserialize<OrderPlaced>(json, Json)!, correlationId, ct);
                break;
            case nameof(StockReleaseRequested):
                await OnReleaseRequested(db, publisher, JsonSerializer.Deserialize<StockReleaseRequested>(json, Json)!, correlationId, ct);
                break;
        }
    }

    private static async Task OnOrderPlaced(InventoryDbContext db, IEventPublisher publisher,
        OrderPlaced evt, string? correlationId, CancellationToken ct)
    {
        if (await db.ProcessedMessages.AnyAsync(m => m.EventId == evt.EventId, ct))
        {
            // Already handled — re-publish the outcome so a lost downstream message still advances the saga.
            var reservedBefore = await db.Reservations.AnyAsync(r => r.OrderId == evt.OrderId && !r.Released, ct);
            await PublishOutcome(publisher, evt.OrderId, reservedBefore, "Insufficient stock", correlationId, ct);
            return;
        }

        await using var tx = await db.Database.BeginTransactionAsync(ct);

        var ok = true;
        var reason = "Insufficient stock";
        foreach (var item in evt.Items)
        {
            var inv = await db.Items.FirstOrDefaultAsync(i => i.ProductId == item.ProductId, ct);
            if (inv is null || inv.QuantityOnHand - inv.Reserved < item.Quantity)
            {
                ok = false;
                reason = inv is null
                    ? $"Unknown product {item.ProductId}"
                    : $"Insufficient stock for product {item.ProductId} (need {item.Quantity}, available {inv.Available})";
                break;
            }
        }

        if (ok)
        {
            foreach (var item in evt.Items)
            {
                var inv = await db.Items.FirstAsync(i => i.ProductId == item.ProductId, ct);
                inv.Reserved += item.Quantity;
                db.Reservations.Add(new Reservation { OrderId = evt.OrderId, ProductId = item.ProductId, Quantity = item.Quantity });
            }
        }

        db.ProcessedMessages.Add(new ProcessedMessage { EventId = evt.EventId, Consumer = "inventory" });
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        await PublishOutcome(publisher, evt.OrderId, ok, reason, correlationId, ct);
    }

    private static async Task PublishOutcome(IEventPublisher publisher, Guid orderId, bool reserved,
        string reason, string? correlationId, CancellationToken ct)
    {
        if (reserved)
            await publisher.PublishAsync(new StockReserved { OrderId = orderId, CorrelationId = correlationId }, ct);
        else
            await publisher.PublishAsync(new StockReservationFailed { OrderId = orderId, Reason = reason, CorrelationId = correlationId }, ct);
    }

    private static async Task OnReleaseRequested(InventoryDbContext db, IEventPublisher publisher,
        StockReleaseRequested evt, string? correlationId, CancellationToken ct)
    {
        if (await db.ProcessedMessages.AnyAsync(m => m.EventId == evt.EventId, ct))
        {
            await publisher.PublishAsync(new StockReleased { OrderId = evt.OrderId, CorrelationId = correlationId }, ct);
            return;
        }

        await using var tx = await db.Database.BeginTransactionAsync(ct);

        var reservations = await db.Reservations.Where(r => r.OrderId == evt.OrderId && !r.Released).ToListAsync(ct);
        foreach (var r in reservations)
        {
            var inv = await db.Items.FirstOrDefaultAsync(i => i.ProductId == r.ProductId, ct);
            if (inv is not null)
                inv.Reserved = Math.Max(0, inv.Reserved - r.Quantity);
            r.Released = true;
        }

        db.ProcessedMessages.Add(new ProcessedMessage { EventId = evt.EventId, Consumer = "inventory" });
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        await publisher.PublishAsync(new StockReleased { OrderId = evt.OrderId, CorrelationId = correlationId }, ct);
    }
}
