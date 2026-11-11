using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Store.Shared.Common.Correlation;
using Store.Shared.Events;

namespace Store.Shared.Common.Messaging;

/// <summary>
/// Base class for a reliable, manual-ack consumer running as a hosted BackgroundService.
/// Declares the main queue with a dead-letter exchange + per-queue ".dlq", binds the
/// routing keys it cares about, and dispatches each message inside a DI scope so handlers
/// can use scoped services (DbContext, publisher, correlation). A handler that throws
/// causes the message to be nacked (requeue:false) and dead-lettered, never silently dropped.
/// Idempotency is the handler's responsibility (via the service's processed-messages table).
/// </summary>
public abstract class RabbitMqConsumer : BackgroundService
{
    protected abstract string QueueName { get; }
    protected abstract string[] RoutingKeys { get; }

    private readonly RabbitMqConnection _conn;
    private readonly IServiceScopeFactory _scopes;
    private readonly RabbitMqOptions _opts;
    private readonly ILogger _log;

    protected RabbitMqConsumer(RabbitMqConnection conn, IServiceScopeFactory scopes,
        Microsoft.Extensions.Options.IOptions<RabbitMqOptions> opts, ILogger log)
    {
        _conn = conn;
        _scopes = scopes;
        _opts = opts.Value;
        _log = log;
    }

    /// <summary>Handle one event. Throw to dead-letter the message.</summary>
    protected abstract Task HandleAsync(IServiceProvider services, string eventName, string json,
        string? correlationId, CancellationToken ct);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var connection = await _conn.GetConnectionAsync(stoppingToken);
                var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

                await SetupTopologyAsync(channel, stoppingToken);
                await channel.BasicQosAsync(0, _opts.Prefetch, global: false, cancellationToken: stoppingToken);

                var consumer = new AsyncEventingBasicConsumer(channel);
                consumer.ReceivedAsync += (_, ea) => OnMessageAsync(channel, ea, stoppingToken);

                await channel.BasicConsumeAsync(QueueName, autoAck: false, consumer: consumer, cancellationToken: stoppingToken);
                _log.LogInformation("Consumer listening on queue {Queue} (keys: {Keys})", QueueName, string.Join(",", RoutingKeys));

                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Consumer on {Queue} crashed; retrying in 5s", QueueName);
                try { await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken); }
                catch (OperationCanceledException) { break; }
            }
        }
    }

    private async Task SetupTopologyAsync(IChannel channel, CancellationToken ct)
    {
        await channel.ExchangeDeclareAsync(EventBus.Exchange, ExchangeType.Direct, durable: true, autoDelete: false, cancellationToken: ct);
        await channel.ExchangeDeclareAsync(EventBus.DeadLetterExchange, ExchangeType.Direct, durable: true, autoDelete: false, cancellationToken: ct);

        var dlq = QueueName + ".dlq";
        await channel.QueueDeclareAsync(dlq, durable: true, exclusive: false, autoDelete: false, cancellationToken: ct);
        await channel.QueueBindAsync(dlq, EventBus.DeadLetterExchange, dlq, cancellationToken: ct);

        var args = new Dictionary<string, object?>
        {
            ["x-dead-letter-exchange"] = EventBus.DeadLetterExchange,
            ["x-dead-letter-routing-key"] = dlq
        };
        await channel.QueueDeclareAsync(QueueName, durable: true, exclusive: false, autoDelete: false, arguments: args, cancellationToken: ct);

        foreach (var rk in RoutingKeys)
            await channel.QueueBindAsync(QueueName, EventBus.Exchange, rk, cancellationToken: ct);
    }

    private async Task OnMessageAsync(IChannel channel, BasicDeliverEventArgs ea, CancellationToken ct)
    {
        var eventName = ea.BasicProperties.Type ?? ea.RoutingKey;
        var correlationId = ea.BasicProperties.CorrelationId;
        var json = Encoding.UTF8.GetString(ea.Body.Span);

        using var scope = _scopes.CreateScope();
        var cc = scope.ServiceProvider.GetService<CorrelationContext>();
        if (cc is not null) cc.CorrelationId = correlationId;

        try
        {
            using (_log.BeginScope(new Dictionary<string, object?>
            {
                ["CorrelationId"] = correlationId,
                ["Event"] = eventName,
                ["Queue"] = QueueName
            }))
            {
                await HandleAsync(scope.ServiceProvider, eventName, json, correlationId, ct);
            }

            await channel.BasicAckAsync(ea.DeliveryTag, multiple: false, ct);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Handler failed for {Event} on {Queue}; dead-lettering", eventName, QueueName);
            try { await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false, ct); }
            catch (Exception nackEx) { _log.LogError(nackEx, "Nack failed on {Queue}", QueueName); }
        }
    }
}
