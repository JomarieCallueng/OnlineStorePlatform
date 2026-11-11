namespace Payment.API.Models;

public record ChargeDto(Guid Id, Guid OrderId, decimal Amount, string Status, DateTime CreatedAt);

public record SimulateFailureRequest(bool Enabled);

public record FaultStateDto(bool FailPayments);
