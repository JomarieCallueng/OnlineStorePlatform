namespace Storefront.UI.Models;

public record ProductDto(Guid Id, string Name, decimal Price, bool IsActive);
public record AuthResponse(string AccessToken, string TokenType, int ExpiresInMinutes, string Username, string Role);
public record StockDto(Guid ProductId, int QuantityOnHand, int Reserved, int Available);
public record OrderLineDto(Guid ProductId, string ProductName, int Quantity, decimal UnitPrice);
public record OrderStatusHistoryDto(string Status, string Note, DateTime Timestamp);
public record OrderDto(Guid Id, Guid CustomerId, string Status, decimal Total, DateTime CreatedAt,
    List<OrderLineDto> Lines, List<OrderStatusHistoryDto> History);
public record FaultStateDto(bool FailPayments);

public record OrderItemRequest(Guid ProductId, int Quantity);
public record PlaceOrderRequest(List<OrderItemRequest> Items);
public record CreateProductRequest(string Name, decimal Price, bool IsActive);
public record UpdateProductRequest(string Name, decimal Price, bool IsActive);
public record AdjustStockRequest(int QuantityOnHand);
public record SimulateFailureRequest(bool Enabled);
