using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Store.Shared.Common.Security;

public static class AuthExtensions
{
    /// <summary>
    /// Adds JWT bearer authentication validating issuer, audience, lifetime and the shared
    /// signing key, with roles mapped from the standard role claim. Used by services and the gateway.
    /// </summary>
    public static IServiceCollection AddStoreJwtAuth(this IServiceCollection services, IConfiguration config)
    {
        var jwt = config.GetSection("Jwt").Get<JwtOptions>() ?? new JwtOptions();
        services.Configure<JwtOptions>(config.GetSection("Jwt"));

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwt.Audience,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
                    RoleClaimType = ClaimTypes.Role,
                    NameClaimType = ClaimTypes.Name,
                    ClockSkew = TimeSpan.FromSeconds(30)
                };
            });

        services.AddAuthorization();
        return services;
    }
}
