namespace Storefront.UI.Services;

/// <summary>Helpers for reading the signed-in user from session.</summary>
public static class SessionExtensions
{
    public const string TokenKey = "access_token";
    public const string UserKey = "username";
    public const string RoleKey = "role";

    public static bool IsLoggedIn(this ISession session) => !string.IsNullOrEmpty(session.GetString(TokenKey));
    public static string? Username(this ISession session) => session.GetString(UserKey);
    public static string? Role(this ISession session) => session.GetString(RoleKey);
    public static bool IsAdmin(this ISession session) => session.GetString(RoleKey) == "Admin";
}
