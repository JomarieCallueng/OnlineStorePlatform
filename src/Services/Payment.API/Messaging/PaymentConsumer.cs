using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Payment.API.Data;
using Payment.API.Models;
using Store.Shared.Common.Data;
using Store.Shared.Common.Messaging;
using Store.Shared.Events;

namespace Payment.API.Messaging;

/// <summary>
/// Charges on PaymentRequested (honouring the runtime failure switch) and refunds on
/// PaymentRefundRequested. Idempotent, with outcome re-publish on redelivery.
/// </summary>
public sealed class PaymentConsumer : RabbitMqConsumer
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    protected override string QueueName => EventBus.Queues.PaymentCharge;
    protected override string[] RoutingKeys => new[] { nameof(PaymentRequested), nameof(PaymentRefundRequested) };

    public PaymentConsumer(RabbitMqConnection conn, IServiceScopeFactory scopes,
        IOptions<RabbitMqOptions> opts, ILogger<PaymentConsumer> log)
        : base(conn, scopes, opts, log) { }

    protected override async Task HandleAsync(IServiceProvider sp, string eventName, string json,
        string? correlationId, CancellationToken ct)
    {
        var db = sp.GetRequiredService<PaymentDbContext>();
        var publisher = sp.GetRequiredService<IEventPublisher>();

        switch (eventName)
        {
            case nameof(PaymentRequested):
                var fault = sp.GetRequiredService<PaymentFaultSwitch>();
                await OnPaymentRequested(db, publisher, fault, JsonSerializer.Deserialize<PaymentRequested>(json, Json)!, correlationId, ct);
                break;
            case nameof(PaymentRefundRequested):
                await OnRefundRequested(db, publisher, JsonSerializer.Deserialize<PaymentRefundRequested>(json, Json)!, correlationId, ct);
                break;
        }
    }

    private static async Task OnPaymentRequested(PaymentDbContext db, IEventPublisher publisher,
        PaymentFaultSwitch fault, PaymentRequested evt, string? correlationId, CancellationToken ct)
    {
        if (await db.ProcessedMessages.AnyAsync(m => m.EventId == evt.EventId, ct))
        {
            var existing = await db.Charges.FirstOrDefaultAsync(c => c.OrderId == evt.OrderId, ct);
            if (existing is { Status: ChargeStatus.Charged })
                await publisher.PublishAsync(new PaymentCharged { OrderId = evt.OrderId, ChargeId = existing.Id, Amount = existing.Amount, CorrelationId = correlationId }, ct);
            else
                await publisher.PublishAsync(new PaymentFailed { OrderId = evt.OrderId, Reason = existing?.FailureReason ?? "Payment declined", CorrelationId = correlationId }, ct);
            return;
        }

        var declined = fault.FailPayments;
        var charge = new Charge
        {
            OrderId = evt.OrderId,
            Amount = evt.Amount,
            Status = declined ? ChargeStatus.Failed : ChargeStatus.Charged,
            FailureReason = declined ? "Simulated payment failure (fault switch enabled)" : null
        };

        await using var tx = await db.Database.BeginTransactionAsync(ct);
        db.Charges.Add(charge);
        db.ProcessedMessages.Add(new ProcessedMessage { EventId = evt.EventId, Consumer = "payment" });
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        if (declined)
            await publisher.PublishAsync(new PaymentFailed { OrderId = evt.OrderId, Reason = charge.FailureReason!, CorrelationId = correlationId }, ct);
        else
            await publisher.PublishAsync(new PaymentCharged { OrderId = evt.OrderId, ChargeId = charge.Id, Amount = charge.Amount, CorrelationId = correlationId }, ct);
    }

    private static async Task OnRefundRequested(PaymentDbContext db, IEventPublisher publisher,
        PaymentRefundRequested evt, string? correlationId, CancellationToken ct)
    {
        if (await db.ProcessedMessages.AnyAsync(m => m.EventId == evt.EventId, ct))
        {
            await publisher.PublishAsync(new PaymentRefunded { OrderId = evt.OrderId, CorrelationId = correlationId }, ct);
            return;
        }

        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var charge = await db.Charges.FirstOrDefaultAsync(c => c.OrderId == evt.OrderId && c.Status == ChargeStatus.Charged, ct);
        if (charge is not null)
            charge.Status = ChargeStatus.Refunded;

        db.ProcessedMessages.Add(new ProcessedMessage { EventId = evt.EventId, Consumer = "payment" });
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        await publisher.PublishAsync(new PaymentRefunded { OrderId = evt.OrderId, CorrelationId = correlationId }, ct);
    }
}
