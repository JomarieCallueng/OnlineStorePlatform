using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using Store.Shared.Common.Correlation;
using Store.Shared.Events;

namespace Store.Shared.Common.Messaging;

/// <summary>
/// Publishes events on the shared direct exchange using a confirm-enabled channel,
/// so a publish only completes once the broker has acknowledged it.
/// </summary>
public sealed class RabbitMqEventPublisher : IEventPublisher
{
    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    private readonly RabbitMqConnection _conn;
    private readonly CorrelationContext _correlation;
    private readonly ILogger<RabbitMqEventPublisher> _log;

    public RabbitMqEventPublisher(RabbitMqConnection conn, CorrelationContext correlation, ILogger<RabbitMqEventPublisher> log)
    {
        _conn = conn;
        _correlation = correlation;
        _log = log;
    }

    public async Task PublishAsync<T>(T @event, CancellationToken ct = default) where T : IntegrationEvent
    {
        var connection = await _conn.GetConnectionAsync(ct);
        await using var channel = await connection.CreateChannelAsync(
            new CreateChannelOptions(publisherConfirmationsEnabled: true, publisherConfirmationTrackingEnabled: true),
            cancellationToken: ct);

        await channel.ExchangeDeclareAsync(EventBus.Exchange, ExchangeType.Direct, durable: true, autoDelete: false,
            cancellationToken: ct);

        var runtimeType = @event.GetType();
        var routingKey = runtimeType.Name;
        var correlationId = @event.CorrelationId ?? _correlation.CorrelationId ?? Guid.NewGuid().ToString();

        var body = JsonSerializer.SerializeToUtf8Bytes(@event, runtimeType, JsonOpts);

        var props = new BasicProperties
        {
            Persistent = true,
            ContentType = "application/json",
            Type = routingKey,
            MessageId = @event.EventId.ToString(),
            CorrelationId = correlationId
        };

        // With publisher confirmations + tracking enabled, this awaits the broker ack
        // and throws if the message is nacked, so callers know the publish was durable.
        await channel.BasicPublishAsync(
            exchange: EventBus.Exchange,
            routingKey: routingKey,
            mandatory: false,
            basicProperties: props,
            body: body,
            cancellationToken: ct);

        _log.LogInformation("Published {Event} ({MessageId}) corr={CorrelationId}", routingKey, props.MessageId, correlationId);
    }
}
