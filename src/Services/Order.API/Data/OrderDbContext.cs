using Microsoft.EntityFrameworkCore;
using Order.API.Models;
using Store.Shared.Common.Data;

namespace Order.API.Data;

public class OrderDbContext : DbContext
{
    public OrderDbContext(DbContextOptions<OrderDbContext> options) : base(options) { }

    public DbSet<Models.Order> Orders => Set<Models.Order>();
    public DbSet<OrderLine> OrderLines => Set<OrderLine>();
    public DbSet<OrderStatusHistory> StatusHistory => Set<OrderStatusHistory>();
    public DbSet<ProcessedMessage> ProcessedMessages => Set<ProcessedMessage>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<Models.Order>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Total).HasPrecision(18, 2);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            e.HasMany(x => x.Lines).WithOne().HasForeignKey(l => l.OrderId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.History).WithOne().HasForeignKey(h => h.OrderId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<OrderLine>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.UnitPrice).HasPrecision(18, 2);
            e.Property(x => x.ProductName).HasMaxLength(200);
        });

        builder.Entity<OrderStatusHistory>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Status).HasMaxLength(20);
            e.Property(x => x.Note).HasMaxLength(400);
        });

        builder.Entity<ProcessedMessage>(e => e.HasKey(x => x.EventId));
    }
}
