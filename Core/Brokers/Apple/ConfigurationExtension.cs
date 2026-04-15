using Microsoft.Extensions.DependencyInjection;

namespace Core.Brokers.Apple;

public static class ConfigurationExtension
{
    public static IServiceCollection AddAppleClient(this IServiceCollection serviceCollection)
    {
        serviceCollection
            .AddHttpClient("apple-jwks",
                client => { client.BaseAddress = new Uri("https://appleid.apple.com/auth/keys"); });

        serviceCollection.AddScoped<AppleClient>();
        return serviceCollection;
    }
}