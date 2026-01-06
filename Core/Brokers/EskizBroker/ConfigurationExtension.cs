using Microsoft.Extensions.DependencyInjection;

namespace Core.Brokers.EskizBroker;

public static class ConfigurationExtension
{
    public static IServiceCollection AddEskizClient(this IServiceCollection serviceCollection)
    {
        serviceCollection
            .AddOptions<EskizConfig>()
            .BindConfiguration("Eskiz")
            .ValidateDataAnnotations()
            .ValidateOnStart();

        serviceCollection.AddSingleton<EskizClient>();

        serviceCollection.AddMemoryCache();

        return serviceCollection;
    }
}