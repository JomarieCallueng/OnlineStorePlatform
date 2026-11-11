using Microsoft.EntityFrameworkCore;
using Payment.API.Models;
using Store.Shared.Common.Data;

namespace Payment.API.Data;

public class PaymentDbContext : DbContext
{
    public PaymentDbContext(DbContextOptions<PaymentDbContext> options) : base(options) { }

    public DbSet<Charge> Charges => Set<Charge>();
    public DbSet<ProcessedMessage> ProcessedMessages => Set<ProcessedMessage>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<Charge>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.OrderId);
            e.Property(x => x.Amount).HasPrecision(18, 2);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        });

        builder.Entity<ProcessedMessage>(e => e.HasKey(x => x.EventId));
    }
}
