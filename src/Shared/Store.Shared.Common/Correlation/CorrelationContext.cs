namespace Store.Shared.Common.Correlation;

/// <summary>Scoped holder for the current correlation id (HTTP request or message handling).</summary>
public sealed class CorrelationContext
{
    public string? CorrelationId { get; set; }
}
