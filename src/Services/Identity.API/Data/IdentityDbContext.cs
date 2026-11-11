using Identity.API.Models;
using Microsoft.EntityFrameworkCore;

namespace Identity.API.Data;

public class IdentityDbContext : DbContext
{
    public IdentityDbContext(DbContextOptions<IdentityDbContext> options) : base(options) { }

    public DbSet<AppUser> Users => Set<AppUser>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<AppUser>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Username).IsUnique();
            e.Property(x => x.Username).HasMaxLength(100).IsRequired();
            e.Property(x => x.PasswordHash).IsRequired();
            e.Property(x => x.Role).HasMaxLength(20).IsRequired();
        });
    }
}
