using System.Security.Claims;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Store.Shared.Common.Security;

/// <summary>Creates signed JWTs (HS256) for the Identity service.</summary>
public static class JwtTokenFactory
{
    public static string Create(JwtOptions options, Guid userId, string username, string role)
    {
        var key = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(options.Key));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var identity = new ClaimsIdentity(new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Name, username),
            new Claim(ClaimTypes.Role, role),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        });

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = options.Issuer,
            Audience = options.Audience,
            Subject = identity,
            Expires = DateTime.UtcNow.AddMinutes(options.ExpiryMinutes),
            SigningCredentials = credentials
        };

        return new JsonWebTokenHandler().CreateToken(descriptor);
    }
}
