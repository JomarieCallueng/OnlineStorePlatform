using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Storefront.UI.Services;

namespace Storefront.UI.Pages.Account;

public class LoginModel : PageModel
{
    private readonly GatewayClient _api;
    public LoginModel(GatewayClient api) => _api = api;

    [BindProperty] public string Username { get; set; } = string.Empty;
    [BindProperty] public string Password { get; set; } = string.Empty;
    public string? ReturnUrl { get; set; }
    public string? Error { get; set; }

    public void OnGet(string? returnUrl = null) => ReturnUrl = returnUrl;

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        ReturnUrl = returnUrl;
        try
        {
            var auth = await _api.LoginAsync(Username, Password);
            HttpContext.Session.SetString(SessionExtensions.TokenKey, auth.AccessToken);
            HttpContext.Session.SetString(SessionExtensions.UserKey, auth.Username);
            HttpContext.Session.SetString(SessionExtensions.RoleKey, auth.Role);
            return LocalRedirect(string.IsNullOrEmpty(returnUrl) ? "/" : returnUrl);
        }
        catch (GatewayException ex)
        {
            Error = ex.Message;
            return Page();
        }
    }
}
