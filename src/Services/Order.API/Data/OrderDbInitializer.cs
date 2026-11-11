using Order.API.Data;
using Store.Shared.Common.Data;

namespace Order.API.Data;

public static class OrderDbInitializer
{
    public static async Task InitializeAsync(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var sp = scope.ServiceProvider;
        var log = sp.GetRequiredService<ILoggerFactory>().CreateLogger("OrderDbInitializer");
        var db = sp.GetRequiredService<OrderDbContext>();
        await DbBootstrap.MigrateOrCreateAsync(db, log);
    }
}
