using System.Net;
using System.Net.Http.Json;
using Order.API.Models;

namespace Order.API.Clients;

/// <summary>
/// Typed client for Catalog. Registered with IHttpClientFactory + the standard resilience
/// handler (retry/backoff/timeout/circuit-breaker). Translates downstream responses into
/// domain results: 404 -> null, other failures -> CatalogUnavailableException.
/// </summary>
public sealed class CatalogClient : ICatalogClient
{
    private readonly HttpClient _http;
    private readonly ILogger<CatalogClient> _log;

    public CatalogClient(HttpClient http, ILogger<CatalogClient> log)
    {
        _http = http;
        _log = log;
    }

    public async Task<CatalogProductDto?> GetProductAsync(Guid id, CancellationToken ct)
    {
        try
        {
            using var resp = await _http.GetAsync($"/catalog/v1/products/{id}", ct);

            if (resp.StatusCode == HttpStatusCode.NotFound)
                return null;

            if (!resp.IsSuccessStatusCode)
                throw new CatalogUnavailableException($"Catalog returned {(int)resp.StatusCode} for product {id}.");

            return await resp.Content.ReadFromJsonAsync<CatalogProductDto>(cancellationToken: ct);
        }
        catch (HttpRequestException ex)
        {
            _log.LogError(ex, "Catalog call failed for product {ProductId}", id);
            throw new CatalogUnavailableException("The catalog service is currently unavailable.", ex);
        }
    }
}
