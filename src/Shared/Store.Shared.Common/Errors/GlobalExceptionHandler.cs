using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Store.Shared.Common.Errors;

/// <summary>
/// Translates exceptions into RFC7807 Problem Details responses with a consistent shape,
/// mapping domain exceptions to 400/404/409 and everything else to 500.
/// </summary>
public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly IProblemDetailsService _problemDetails;
    private readonly ILogger<GlobalExceptionHandler> _log;

    public GlobalExceptionHandler(IProblemDetailsService problemDetails, ILogger<GlobalExceptionHandler> log)
    {
        _problemDetails = problemDetails;
        _log = log;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (status, title) = exception switch
        {
            NotFoundException => (StatusCodes.Status404NotFound, "Resource not found"),
            ConflictException => (StatusCodes.Status409Conflict, "Conflict"),
            StoreValidationException => (StatusCodes.Status400BadRequest, "Invalid request"),
            ArgumentException => (StatusCodes.Status400BadRequest, "Invalid request"),
            UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "Unauthorized"),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred")
        };

        _log.Log(status >= 500 ? LogLevel.Error : LogLevel.Warning, exception,
            "Request failed ({Status}): {Message}", status, exception.Message);

        httpContext.Response.StatusCode = status;

        return await _problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = new ProblemDetails
            {
                Status = status,
                Title = title,
                Detail = status >= 500 ? "See server logs for details." : exception.Message,
                Type = $"https://httpstatuses.io/{status}"
            }
        });
    }
}
