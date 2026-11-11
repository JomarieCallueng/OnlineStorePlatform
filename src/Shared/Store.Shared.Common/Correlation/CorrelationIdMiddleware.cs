using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Store.Shared.Common.Correlation;

/// <summary>
/// Reads (or mints) an X-Correlation-ID at the edge, stores it in the scoped
/// CorrelationContext, echoes it on the response, and opens a logging scope so every
/// log line for the request carries the id.
/// </summary>
public sealed class CorrelationIdMiddleware
{
    public const string HeaderName = "X-Correlation-ID";

    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationIdMiddleware> _log;

    public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> log)
    {
        _next = next;
        _log = log;
    }

    public async Task Invoke(HttpContext context, CorrelationContext correlation)
    {
        var cid = context.Request.Headers[HeaderName].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(cid))
            cid = Guid.NewGuid().ToString();

        correlation.CorrelationId = cid;
        context.Response.Headers[HeaderName] = cid;

        using (_log.BeginScope(new Dictionary<string, object?> { ["CorrelationId"] = cid }))
        {
            await _next(context);
        }
    }
}
