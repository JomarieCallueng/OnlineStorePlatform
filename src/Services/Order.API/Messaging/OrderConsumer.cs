using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Order.API.Data;
using Order.API.Models;
using Store.Shared.Common.Data;
using Store.Shared.Common.Messaging;
using Store.Shared.Events;

namespace Order.API.Messaging;

/// <summary>
/// Orchestrated saga for an order: reserve -> charge -> confirm, with compensation
/// (release stock + refund -> Cancelled) on failure. State transitions are guarded so
/// out-of-order or duplicate events cannot corrupt the order; idempotency is enforced via
/// the processed-messages table, and the command for the current state is (re)published so
/// a lost downstream message still advances the saga.
/// </summary>
public sealed class OrderConsumer : RabbitMqConsumer
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    protected override string QueueName => EventBus.Queues.OrderSaga;
    protected override string[] RoutingKeys => new[]
    {
        nameof(StockReserved), nameof(StockReservationFailed),
        nameof(PaymentCharged), nameof(PaymentFailed),
        nameof(StockReleased), nameof(PaymentRefunded)
    };

    public OrderConsumer(RabbitMqConnection conn, IServiceScopeFactory scopes,
        IOptions<RabbitMqOptions> opts, ILogger<OrderConsumer> log)
        : base(conn, scopes, opts, log) { }

    private static T Deserialize<T>(string json) => JsonSerializer.Deserialize<T>(json, Json)!;

    protected override async Task HandleAsync(IServiceProvider sp, string eventName, string json,
        string? correlationId, CancellationToken ct)
    {
        var db = sp.GetRequiredService<OrderDbContext>();
        var publisher = sp.GetRequiredService<IEventPublisher>();

        Guid eventId, orderId;
        Action<Models.Order> transition;

        switch (eventName)
        {
            case nameof(StockReserved):
            {
                var e = Deserialize<StockReserved>(json); eventId = e.EventId; orderId = e.OrderId;
                transition = o => { if (o.Status == OrderStatus.Pending) SetStatus(o, OrderStatus.StockReserved, "Stock reserved; requesting payment"); };
                break;
            }
            case nameof(StockReservationFailed):
            {
                var e = Deserialize<StockReservationFailed>(json); eventId = e.EventId; orderId = e.OrderId;
                transition = o => { if (o.Status == OrderStatus.Pending) SetStatus(o, OrderStatus.Cancelled, $"Cancelled: {e.Reason}"); };
                break;
            }
            case nameof(PaymentCharged):
            {
                var e = Deserialize<PaymentCharged>(json); eventId = e.EventId; orderId = e.OrderId;
                transition = o => { if (o.Status == OrderStatus.StockReserved) SetStatus(o, OrderStatus.Confirmed, "Payment charged; order confirmed"); };
                break;
            }
            case nameof(PaymentFailed):
            {
                var e = Deserialize<PaymentFailed>(json); eventId = e.EventId; orderId = e.OrderId;
                transition = o => { if (o.Status == OrderStatus.StockReserved) SetStatus(o, OrderStatus.Compensating, $"Payment failed: {e.Reason}; compensating"); };
                break;
            }
            case nameof(StockReleased):
            {
                var e = Deserialize<StockReleased>(json); eventId = e.EventId; orderId = e.OrderId;
                transition = o => { if (o.Status == OrderStatus.Compensating) SetStatus(o, OrderStatus.Cancelled, "Stock released; order cancelled"); };
                break;
            }
            case nameof(PaymentRefunded):
            {
                var e = Deserialize<PaymentRefunded>(json); eventId = e.EventId; orderId = e.OrderId;
                transition = o => AddHistory(o, "Refund", "Payment refunded");
                break;
            }
            default:
                return;
        }

        if (await db.ProcessedMessages.AnyAsync(m => m.EventId == eventId, ct))
        {
            var existing = await db.Orders.Include(o => o.History).FirstOrDefaultAsync(o => o.Id == orderId, ct);
            if (existing is not null)
                await PublishForState(publisher, existing, correlationId, ct);
            return;
        }

        var order = await db.Orders.Include(o => o.History).FirstOrDefaultAsync(o => o.Id == orderId, ct);
        if (order is not null)
            transition(order);

        db.ProcessedMessages.Add(new ProcessedMessage { EventId = eventId, Consumer = "order" });
        await db.SaveChangesAsync(ct);

        if (order is not null)
            await PublishForState(publisher, order, correlationId, ct);
    }

    /// <summary>Emit the command the saga owes for the order's current state (idempotent downstream).</summary>
    private static async Task PublishForState(IEventPublisher publisher, Models.Order order, string? correlationId, CancellationToken ct)
    {
        switch (order.Status)
        {
            case OrderStatus.StockReserved:
                await publisher.PublishAsync(new PaymentRequested
                {
                    OrderId = order.Id,
                    CustomerId = order.CustomerId,
                    Amount = order.Total,
                    CorrelationId = correlationId
                }, ct);
                break;

            case OrderStatus.Compensating:
                await publisher.PublishAsync(new StockReleaseRequested { OrderId = order.Id, CorrelationId = correlationId }, ct);
                await publisher.PublishAsync(new PaymentRefundRequested { OrderId = order.Id, CorrelationId = correlationId }, ct);
                break;
        }
    }

    private static void SetStatus(Models.Order order, OrderStatus status, string note)
    {
        order.Status = status;
        AddHistory(order, status.ToString(), note);
    }

    private static void AddHistory(Models.Order order, string status, string note)
        => order.History.Add(new OrderStatusHistory { OrderId = order.Id, Status = status, Note = note });
}
