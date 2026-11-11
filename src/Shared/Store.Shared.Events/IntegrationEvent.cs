namespace Store.Shared.Events;

/// <summary>
/// Base type for every integration event published on the message bus.
/// Carries an identity (for idempotency) and a correlation id (for end-to-end tracing).
/// </summary>
public abstract record IntegrationEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;

    /// <summary>Correlation id flowing across HTTP + messaging so one request can be traced end to end.</summary>
    public string? CorrelationId { get; init; }
}
