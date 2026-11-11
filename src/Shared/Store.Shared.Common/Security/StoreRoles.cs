namespace Store.Shared.Common.Security;

/// <summary>Shared role names used in [Authorize(Roles = ...)] across services and the gateway.</summary>
public static class StoreRoles
{
    public const string Customer = "Customer";
    public const string Admin = "Admin";
}
