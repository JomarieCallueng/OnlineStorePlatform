using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Storefront.UI.Services;

namespace Storefront.UI.Pages.Admin;

public class PaymentModel : PageModel
{
    private readonly GatewayClient _api;
    public PaymentModel(GatewayClient api) => _api = api;

    public bool FailPayments { get; private set; }
    public string? Error { get; set; }
    [TempData] public string? Message { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (!HttpContext.Session.IsAdmin())
            return RedirectToPage("/Account/Login", new { returnUrl = "/Admin/Payment" });
        try { FailPayments = (await _api.GetFailureStateAsync()).FailPayments; }
        catch (GatewayException ex) { Error = ex.Message; }
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(bool enabled)
    {
        if (!HttpContext.Session.IsAdmin()) return RedirectToPage("/Account/Login");
        try
        {
            var state = await _api.SetFailureStateAsync(enabled);
            Message = state.FailPayments
                ? "Payment failures are now ON — the next order will be declined and compensated."
                : "Payment failures are now OFF — orders will be charged normally.";
        }
        catch (GatewayException ex) { Error = ex.Message; }
        return RedirectToPage();
    }
}
