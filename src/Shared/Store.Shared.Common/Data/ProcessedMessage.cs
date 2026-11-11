namespace Store.Shared.Common.Data;

/// <summary>
/// Idempotency ledger row: records that a given integration event has already been handled
/// by this service, so a redelivered message is not applied twice. Each service owns its own
/// table in its own database.
/// </summary>
public class ProcessedMessage
{
    public Guid EventId { get; set; }
    public string Consumer { get; set; } = string.Empty;
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
}
