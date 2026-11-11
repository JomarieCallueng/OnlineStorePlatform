using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Storefront.UI.Models;
using Storefront.UI.Services;

namespace Storefront.UI.Pages.Admin;

public class InventoryModel : PageModel
{
    private readonly GatewayClient _api;
    public InventoryModel(GatewayClient api) => _api = api;

    public List<StockDto> Stock { get; private set; } = new();
    public List<ProductDto> Products { get; private set; } = new();

    [TempData] public string? Message { get; set; }
    [TempData] public string? Error { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (!HttpContext.Session.IsAdmin())
            return RedirectToPage("/Account/Login", new { returnUrl = "/Admin/Inventory" });
        try
        {
            Stock = await _api.GetStockAsync();
            Products = await _api.GetProductsAsync(includeInactive: true);
        }
        catch (GatewayException ex) { Error = ex.Message; }
        return Page();
    }

    public async Task<IActionResult> OnPostAdjustAsync(Guid productId, int quantityOnHand)
    {
        if (!HttpContext.Session.IsAdmin()) return RedirectToPage("/Account/Login");
        try
        {
            await _api.AdjustStockAsync(productId, quantityOnHand);
            Message = "Stock updated.";
        }
        catch (GatewayException ex) { Error = ex.Message; }
        return RedirectToPage();
    }

    public string NameFor(Guid id) => Products.FirstOrDefault(p => p.Id == id)?.Name ?? id.ToString().Substring(0, 8);
}
