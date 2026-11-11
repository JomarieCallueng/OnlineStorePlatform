using Catalog.API.Models;
using Microsoft.EntityFrameworkCore;
using Store.Shared.Common.Data;
using Store.Shared.Events;

namespace Catalog.API.Data;

public static class CatalogDbInitializer
{
    public static async Task InitializeAsync(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var sp = scope.ServiceProvider;
        var log = sp.GetRequiredService<ILoggerFactory>().CreateLogger("CatalogDbInitializer");
        var db = sp.GetRequiredService<CatalogDbContext>();

        await DbBootstrap.MigrateOrCreateAsync(db, log);

        if (!await db.Products.AnyAsync())
        {
            db.Products.AddRange(SeedProducts.All.Select(p => new Product
            {
                Id = p.Id,
                Name = p.Name,
                Price = p.Price,
                IsActive = true
            }));
            await db.SaveChangesAsync();
            log.LogInformation("Seeded {Count} catalog products.", SeedProducts.All.Count);
        }
    }
}
