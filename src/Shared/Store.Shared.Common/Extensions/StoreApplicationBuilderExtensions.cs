using Microsoft.AspNetCore.Builder;
using Store.Shared.Common.Correlation;

namespace Store.Shared.Common.Extensions;

public static class StoreApplicationBuilderExtensions
{
    public static IApplicationBuilder UseStoreCorrelation(this IApplicationBuilder app)
        => app.UseMiddleware<CorrelationIdMiddleware>();
}
