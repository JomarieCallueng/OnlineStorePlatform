using Inventory.API.Models;
using Microsoft.EntityFrameworkCore;
using Store.Shared.Common.Data;

namespace Inventory.API.Data;

public class InventoryDbContext : DbContext
{
    public InventoryDbContext(DbContextOptions<InventoryDbContext> options) : base(options) { }

    public DbSet<InventoryItem> Items => Set<InventoryItem>();
    public DbSet<Reservation> Reservations => Set<Reservation>();
    public DbSet<ProcessedMessage> ProcessedMessages => Set<ProcessedMessage>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<InventoryItem>(e =>
        {
            e.HasKey(x => x.ProductId);
            e.Property(x => x.ProductId).ValueGeneratedNever();
        });

        builder.Entity<Reservation>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.OrderId);
        });

        builder.Entity<ProcessedMessage>(e =>
        {
            e.HasKey(x => x.EventId);
        });
    }
}
