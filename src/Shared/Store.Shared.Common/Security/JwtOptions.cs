namespace Store.Shared.Common.Security;

public sealed class JwtOptions
{
    public string Key { get; set; } = "";
    public string Issuer { get; set; } = "store-identity";
    public string Audience { get; set; } = "store-clients";
    public int ExpiryMinutes { get; set; } = 120;
}
