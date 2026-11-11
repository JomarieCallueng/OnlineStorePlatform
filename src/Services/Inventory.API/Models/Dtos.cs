using System.ComponentModel.DataAnnotations;

namespace Inventory.API.Models;

public record StockDto(Guid ProductId, int QuantityOnHand, int Reserved, int Available);

public record AdjustStockRequest([Range(0, 1_000_000)] int QuantityOnHand);
