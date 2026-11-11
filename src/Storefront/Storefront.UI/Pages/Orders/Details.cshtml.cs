using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Storefront.UI.Models;
using Storefront.UI.Services;

namespace Storefront.UI.Pages.Orders;

public class DetailsModel : PageModel
{
    private readonly GatewayClient _api;
    public DetailsModel(GatewayClient api) => _api = api;

    public OrderDto? Order { get; private set; }
    public string? Error { get; set; }

    public bool InProgress => Order is not null &&
        Order.Status is "Pending" or "StockReserved" or "Compensating";

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        if (!HttpContext.Session.IsLoggedIn())
            return RedirectToPage("/Account/Login");

        try { Order = await _api.GetOrderAsync(id); }
        catch (GatewayException ex) { Error = ex.Message; }
        return Page();
    }
}
