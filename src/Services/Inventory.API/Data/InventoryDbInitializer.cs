using Inventory.API.Models;
using Microsoft.EntityFrameworkCore;
using Store.Shared.Common.Data;
using Store.Shared.Events;

namespace Inventory.API.Data;

public static class InventoryDbInitializer
{
    public static async Task InitializeAsync(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var sp = scope.ServiceProvider;
        var log = sp.GetRequiredService<ILoggerFactory>().CreateLogger("InventoryDbInitializer");
        var db = sp.GetRequiredService<InventoryDbContext>();

        await DbBootstrap.MigrateOrCreateAsync(db, log);

        if (!await db.Items.AnyAsync())
        {
            db.Items.AddRange(SeedProducts.All.Select(p => new InventoryItem
            {
                ProductId = p.Id,
                QuantityOnHand = p.Stock,
                Reserved = 0
            }));
            await db.SaveChangesAsync();
            log.LogInformation("Seeded stock for {Count} products.", SeedProducts.All.Count);
        }
    }
}
