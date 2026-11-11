using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Ocelot route table (downstream hosts are the compose service names).
builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);

var jwtKey = builder.Configuration["Jwt:Key"] ?? string.Empty;
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "store-identity";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "store-clients";

// The gateway validates the JWT before forwarding. The scheme key "Bearer" is referenced
// by protected routes in ocelot.json via AuthenticationProviderKey.
builder.Services.AddAuthentication()
    .AddJwtBearer("Bearer", options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            RoleClaimType = ClaimTypes.Role,
            NameClaimType = ClaimTypes.Name,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

builder.Services.AddOcelot(builder.Configuration);
builder.Services.AddHealthChecks();

var app = builder.Build();

// Answer /health at the gateway itself, before handing the pipeline to Ocelot.
app.Map("/health", branch => branch.Run(async ctx =>
{
    ctx.Response.StatusCode = StatusCodes.Status200OK;
    await ctx.Response.WriteAsync("Healthy");
}));

await app.UseOcelot();

app.Run();
