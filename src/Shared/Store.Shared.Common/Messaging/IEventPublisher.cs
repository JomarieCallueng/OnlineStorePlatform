using Store.Shared.Events;

namespace Store.Shared.Common.Messaging;

/// <summary>Publishes integration events to the broker with publisher confirms.</summary>
public interface IEventPublisher
{
    Task PublishAsync<T>(T @event, CancellationToken ct = default) where T : IntegrationEvent;
}
