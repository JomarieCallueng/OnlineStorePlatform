namespace Order.API.Models;

public enum OrderStatus
{
    Pending,        // created; OrderPlaced published
    StockReserved,  // inventory reserved; payment requested
    Confirmed,      // payment charged; terminal success
    Compensating,   // payment failed; releasing stock / refunding
    Cancelled       // compensation complete; terminal failure
}

public class Order
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CustomerId { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public decimal Total { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<OrderLine> Lines { get; set; } = new();
    public List<OrderStatusHistory> History { get; set; } = new();
}

public class OrderLine
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OrderId { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}

public class OrderStatusHistory
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OrderId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Note { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
