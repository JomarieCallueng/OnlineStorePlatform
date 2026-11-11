using Identity.API.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Store.Shared.Common.Data;

namespace Identity.API.Data;

public static class IdentityDbInitializer
{
    public static async Task InitializeAsync(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var sp = scope.ServiceProvider;
        var log = sp.GetRequiredService<ILoggerFactory>().CreateLogger("IdentityDbInitializer");
        var db = sp.GetRequiredService<IdentityDbContext>();

        await DbBootstrap.MigrateOrCreateAsync(db, log);

        if (!await db.Users.AnyAsync(u => u.Role == Roles.Admin))
        {
            var cfg = sp.GetRequiredService<IConfiguration>();
            var hasher = sp.GetRequiredService<IPasswordHasher<AppUser>>();

            var admin = new AppUser { Username = cfg["Seed:AdminUsername"] ?? "admin", Role = Roles.Admin };
            admin.PasswordHash = hasher.HashPassword(admin, cfg["Seed:AdminPassword"] ?? "Admin123!");

            var customer = new AppUser { Username = "customer", Role = Roles.Customer };
            customer.PasswordHash = hasher.HashPassword(customer, "Customer123!");

            db.Users.AddRange(admin, customer);
            await db.SaveChangesAsync();
            log.LogInformation("Seeded admin and demo customer users.");
        }
    }
}
