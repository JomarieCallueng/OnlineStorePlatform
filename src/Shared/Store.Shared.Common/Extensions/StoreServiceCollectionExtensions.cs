using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Store.Shared.Common.Correlation;
using Store.Shared.Common.Errors;
using Store.Shared.Common.Messaging;

namespace Store.Shared.Common.Extensions;

public static class StoreServiceCollectionExtensions
{
    /// <summary>Correlation context + outbound propagation handler.</summary>
    public static IServiceCollection AddStoreCorrelation(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<CorrelationContext>();
        services.AddTransient<CorrelationIdDelegatingHandler>();
        return services;
    }

    /// <summary>RabbitMQ connection + confirm-enabled event publisher.</summary>
    public static IServiceCollection AddStoreMessaging(this IServiceCollection services, IConfiguration config)
    {
        services.Configure<RabbitMqOptions>(config.GetSection("RabbitMq"));
        services.AddSingleton<RabbitMqConnection>();
        services.AddScoped<IEventPublisher, RabbitMqEventPublisher>();
        return services;
    }

    /// <summary>Problem Details + global exception handler.</summary>
    public static IServiceCollection AddStoreProblemDetails(this IServiceCollection services)
    {
        services.AddProblemDetails();
        services.AddExceptionHandler<GlobalExceptionHandler>();
        return services;
    }
}
