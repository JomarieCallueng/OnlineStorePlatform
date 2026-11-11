using Order.API.Models;

namespace Order.API.Clients;

public interface ICatalogClient
{
    /// <summary>Returns the product, or null if the catalog reports 404.</summary>
    Task<CatalogProductDto?> GetProductAsync(Guid id, CancellationToken ct);
}

/// <summary>Raised when the catalog is unreachable or returns an unexpected error (after resilience).</summary>
public sealed class CatalogUnavailableException : Exception
{
    public CatalogUnavailableException(string message, Exception? inner = null) : base(message, inner) { }
}
