namespace Store.Shared.Events;

/// <summary>
/// Shared broker topology constants so publishers and consumers agree on names.
/// A single durable direct exchange is used; the routing key is the event type name.
/// Each consumer queue dead-letters to a per-queue ".dlq" via the dead-letter exchange.
/// </summary>
public static class EventBus
{
    public const string Exchange = "store.events";
    public const string DeadLetterExchange = "store.events.dlx";

    public static class Queues
    {
        public const string InventoryReserve = "inventory.reserve";
        public const string PaymentCharge = "payment.charge";
        public const string OrderSaga = "order.saga";
    }

    /// <summary>Routing key for an event is simply its CLR type name.</summary>
    public static string RoutingKey<T>() where T : IntegrationEvent => typeof(T).Name;
    public static string RoutingKey(IntegrationEvent e) => e.GetType().Name;
}
