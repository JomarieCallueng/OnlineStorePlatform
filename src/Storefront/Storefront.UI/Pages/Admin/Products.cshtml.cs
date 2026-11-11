using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Storefront.UI.Models;
using Storefront.UI.Services;

namespace Storefront.UI.Pages.Admin;

public class ProductsModel : PageModel
{
    private readonly GatewayClient _api;
    public ProductsModel(GatewayClient api) => _api = api;

    public List<ProductDto> Products { get; private set; } = new();

    [TempData] public string? Message { get; set; }
    [TempData] public string? Error { get; set; }

    [BindProperty] public string Name { get; set; } = string.Empty;
    [BindProperty] public decimal Price { get; set; }
    [BindProperty] public bool IsActive { get; set; } = true;

    public async Task<IActionResult> OnGetAsync()
    {
        if (!HttpContext.Session.IsAdmin())
            return RedirectToPage("/Account/Login", new { returnUrl = "/Admin/Products" });

        try { Products = await _api.GetProductsAsync(includeInactive: true); }
        catch (GatewayException ex) { Error = ex.Message; }
        return Page();
    }

    public async Task<IActionResult> OnPostCreateAsync()
    {
        if (!HttpContext.Session.IsAdmin()) return RedirectToPage("/Account/Login");
        try
        {
            await _api.CreateProductAsync(new CreateProductRequest(Name, Price, IsActive));
            Message = $"Created '{Name}'.";
        }
        catch (GatewayException ex) { Error = ex.Message; }
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostUpdateAsync(Guid id, string name, decimal price, bool isActive)
    {
        if (!HttpContext.Session.IsAdmin()) return RedirectToPage("/Account/Login");
        try
        {
            await _api.UpdateProductAsync(id, new UpdateProductRequest(name, price, isActive));
            Message = $"Updated '{name}'.";
        }
        catch (GatewayException ex) { Error = ex.Message; }
        return RedirectToPage();
    }
}
