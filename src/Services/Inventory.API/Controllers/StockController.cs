using Asp.Versioning;
using Inventory.API.Data;
using Inventory.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Store.Shared.Common.Errors;
using Store.Shared.Common.Security;

namespace Inventory.API.Controllers;

/// <summary>Admin stock management. All actions require the Admin role.</summary>
[ApiController]
[ApiVersion("1.0")]
[Route("inventory/v{version:apiVersion}/stock")]
[Produces("application/json")]
[Authorize(Roles = StoreRoles.Admin)]
public class StockController : ControllerBase
{
    private readonly InventoryDbContext _db;

    public StockController(InventoryDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<StockDto>>> List()
    {
        var items = await _db.Items.AsNoTracking()
            .Select(i => new StockDto(i.ProductId, i.QuantityOnHand, i.Reserved, i.QuantityOnHand - i.Reserved))
            .ToListAsync();
        return Ok(items);
    }

    [HttpGet("{productId:guid}")]
    public async Task<ActionResult<StockDto>> Get(Guid productId)
    {
        var i = await _db.Items.AsNoTracking().FirstOrDefaultAsync(x => x.ProductId == productId)
                ?? throw new NotFoundException($"No stock record for product {productId}.");
        return Ok(new StockDto(i.ProductId, i.QuantityOnHand, i.Reserved, i.Available));
    }

    /// <summary>Set the absolute quantity on hand for a product (restock/correction).</summary>
    [HttpPut("{productId:guid}")]
    public async Task<ActionResult<StockDto>> Adjust(Guid productId, AdjustStockRequest req)
    {
        var item = await _db.Items.FirstOrDefaultAsync(x => x.ProductId == productId);
        if (item is null)
        {
            item = new InventoryItem { ProductId = productId, QuantityOnHand = req.QuantityOnHand, Reserved = 0 };
            _db.Items.Add(item);
        }
        else
        {
            if (req.QuantityOnHand < item.Reserved)
                throw new StoreValidationException($"Quantity on hand ({req.QuantityOnHand}) cannot be less than currently reserved ({item.Reserved}).");
            item.QuantityOnHand = req.QuantityOnHand;
        }

        await _db.SaveChangesAsync();
        return Ok(new StockDto(item.ProductId, item.QuantityOnHand, item.Reserved, item.Available));
    }
}
