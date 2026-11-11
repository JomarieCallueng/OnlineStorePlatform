using System.ComponentModel.DataAnnotations;

namespace Order.API.Models;

public record OrderItemRequest([Required] Guid ProductId, [Range(1, 10_000)] int Quantity);

public record PlaceOrderRequest([Required][MinLength(1)] List<OrderItemRequest> Items);

public record OrderLineDto(Guid ProductId, string ProductName, int Quantity, decimal UnitPrice);

public record OrderStatusHistoryDto(string Status, string Note, DateTime Timestamp);

public record OrderDto(
    Guid Id,
    Guid CustomerId,
    string Status,
    decimal Total,
    DateTime CreatedAt,
    IReadOnlyList<OrderLineDto> Lines,
    IReadOnlyList<OrderStatusHistoryDto> History);

/// <summary>Shape returned by the Catalog service (consumed by the typed client).</summary>
public record CatalogProductDto(Guid Id, string Name, decimal Price, bool IsActive);
