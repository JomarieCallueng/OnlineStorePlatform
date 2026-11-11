using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Store.Shared.Common.Data;

/// <summary>
/// Brings a database up at startup, waiting for the server to accept connections.
/// If the service assembly carries EF Core migrations it applies them (the intended path);
/// if none are present yet it falls back to EnsureCreated so a fresh clone still runs.
/// </summary>
public static class DbBootstrap
{
    public static async Task MigrateOrCreateAsync(DbContext db, ILogger? log = null, int retries = 12, int delaySeconds = 5)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                if (db.Database.GetMigrations().Any())
                    await db.Database.MigrateAsync();
                else
                    await db.Database.EnsureCreatedAsync();

                log?.LogInformation("Database ready for {Context}", db.GetType().Name);
                return;
            }
            catch (Exception ex) when (attempt < retries)
            {
                log?.LogWarning("Database not ready for {Context} (attempt {Attempt}/{Retries}): {Message}; retrying in {Delay}s",
                    db.GetType().Name, attempt, retries, ex.Message, delaySeconds);
                await Task.Delay(TimeSpan.FromSeconds(delaySeconds));
            }
        }
    }
}
