using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Storefront.UI.Models;
using Storefront.UI.Services;

namespace Storefront.UI.Pages;

public class IndexModel : PageModel
{
    private readonly GatewayClient _api;
    public IndexModel(GatewayClient api) => _api = api;

    public List<ProductDto> Products { get; private set; } = new();

    [TempData] public string? Error { get; set; }

    public async Task OnGetAsync()
    {
        try { Products = await _api.GetProductsAsync(); }
        catch (GatewayException ex) { Error = ex.Message; }
    }

    public async Task<IActionResult> OnPostOrderAsync(Guid productId, int quantity)
    {
        if (!HttpContext.Session.IsLoggedIn())
            return RedirectToPage("/Account/Login", new { returnUrl = "/" });

        if (quantity < 1) quantity = 1;

        try
        {
            var order = await _api.PlaceOrderAsync(new PlaceOrderRequest(new() { new OrderItemRequest(productId, quantity) }));
            return RedirectToPage("/Orders/Details", new { id = order.Id });
        }
        catch (GatewayException ex)
        {
            Error = ex.Message;
            return RedirectToPage();
        }
    }
}
