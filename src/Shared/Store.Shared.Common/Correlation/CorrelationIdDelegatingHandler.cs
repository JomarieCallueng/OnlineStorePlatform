using Store.Shared.Common.Correlation;

namespace Store.Shared.Common.Correlation;

/// <summary>
/// Propagates the current correlation id on outbound HttpClient calls so a request can be
/// traced across service boundaries.
/// </summary>
public sealed class CorrelationIdDelegatingHandler : DelegatingHandler
{
    private readonly CorrelationContext _correlation;

    public CorrelationIdDelegatingHandler(CorrelationContext correlation) => _correlation = correlation;

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var cid = _correlation.CorrelationId;
        if (!string.IsNullOrWhiteSpace(cid) && !request.Headers.Contains(CorrelationIdMiddleware.HeaderName))
            request.Headers.Add(CorrelationIdMiddleware.HeaderName, cid);

        return base.SendAsync(request, cancellationToken);
    }
}
