using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Storefront.UI.Services;

namespace Storefront.UI.Pages.Account;

public class RegisterModel : PageModel
{
    private readonly GatewayClient _api;
    public RegisterModel(GatewayClient api) => _api = api;

    [BindProperty] public string Username { get; set; } = string.Empty;
    [BindProperty] public string Password { get; set; } = string.Empty;
    public string? Error { get; set; }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        try
        {
            var auth = await _api.RegisterAsync(Username, Password);
            HttpContext.Session.SetString(SessionExtensions.TokenKey, auth.AccessToken);
            HttpContext.Session.SetString(SessionExtensions.UserKey, auth.Username);
            HttpContext.Session.SetString(SessionExtensions.RoleKey, auth.Role);
            return RedirectToPage("/Index");
        }
        catch (GatewayException ex)
        {
            Error = ex.Message;
            return Page();
        }
    }
}
