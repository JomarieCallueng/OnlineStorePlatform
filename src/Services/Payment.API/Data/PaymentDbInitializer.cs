using Payment.API.Data;
using Store.Shared.Common.Data;

namespace Payment.API.Data;

public static class PaymentDbInitializer
{
    public static async Task InitializeAsync(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var sp = scope.ServiceProvider;
        var log = sp.GetRequiredService<ILoggerFactory>().CreateLogger("PaymentDbInitializer");
        var db = sp.GetRequiredService<PaymentDbContext>();
        await DbBootstrap.MigrateOrCreateAsync(db, log);
    }
}
