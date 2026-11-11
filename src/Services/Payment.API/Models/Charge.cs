namespace Payment.API.Models;

public enum ChargeStatus
{
    Charged,
    Refunded,
    Failed
}

public class Charge
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OrderId { get; set; }
    public decimal Amount { get; set; }
    public ChargeStatus Status { get; set; } = ChargeStatus.Charged;
    public string? FailureReason { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
