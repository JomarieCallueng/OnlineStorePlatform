namespace Store.Shared.Events;

/// <summary>A single line of an order (shared value used across services).</summary>
public record OrderItem(Guid ProductId, int Quantity, decimal UnitPrice);

// ---- Saga start -------------------------------------------------------------

/// <summary>Published by Order once an order row exists; starts the saga.</summary>
public record OrderPlaced : IntegrationEvent
{
    public Guid OrderId { get; init; }
    public Guid CustomerId { get; init; }
    public IReadOnlyList<OrderItem> Items { get; init; } = Array.Empty<OrderItem>();
    public decimal Total { get; init; }
}

// ---- Inventory step ---------------------------------------------------------

public record StockReserved : IntegrationEvent
{
    public Guid OrderId { get; init; }
}

public record StockReservationFailed : IntegrationEvent
{
    public Guid OrderId { get; init; }
    public string Reason { get; init; } = "Insufficient stock";
}

/// <summary>Compensation command: release any stock reserved for this order.</summary>
public record StockReleaseRequested : IntegrationEvent
{
    public Guid OrderId { get; init; }
}

public record StockReleased : IntegrationEvent
{
    public Guid OrderId { get; init; }
}

// ---- Payment step -----------------------------------------------------------

/// <summary>Order asks Payment to charge once stock is reserved.</summary>
public record PaymentRequested : IntegrationEvent
{
    public Guid OrderId { get; init; }
    public Guid CustomerId { get; init; }
    public decimal Amount { get; init; }
}

public record PaymentCharged : IntegrationEvent
{
    public Guid OrderId { get; init; }
    public Guid ChargeId { get; init; }
    public decimal Amount { get; init; }
}

public record PaymentFailed : IntegrationEvent
{
    public Guid OrderId { get; init; }
    public string Reason { get; init; } = "Payment declined";
}

/// <summary>Compensation command: refund a charge already taken for this order.</summary>
public record PaymentRefundRequested : IntegrationEvent
{
    public Guid OrderId { get; init; }
}

public record PaymentRefunded : IntegrationEvent
{
    public Guid OrderId { get; init; }
}
