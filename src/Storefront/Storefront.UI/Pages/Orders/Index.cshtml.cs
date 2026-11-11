using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Storefront.UI.Models;
using Storefront.UI.Services;

namespace Storefront.UI.Pages.Orders;

public class IndexModel : PageModel
{
    private readonly GatewayClient _api;
    public IndexModel(GatewayClient api) => _api = api;

    public List<OrderDto> Orders { get; private set; } = new();
    public string? Error { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (!HttpContext.Session.IsLoggedIn())
            return RedirectToPage("/Account/Login", new { returnUrl = "/Orders" });

        try { Orders = await _api.GetOrdersAsync(); }
        catch (GatewayException ex) { Error = ex.Message; }
        return Page();
    }
}
