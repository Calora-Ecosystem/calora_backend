using Microsoft.Extensions.DependencyInjection;

namespace Core.Brokers.EmailBroker;

public static class ConfigurationExtension
{
    public static IServiceCollection AddEmailClient(this IServiceCollection serviceCollection)
    {
        serviceCollection
            .AddOptions<EmailConfig>()
            .BindConfiguration("Email")
            .ValidateOnStart();

        serviceCollection.AddTransient<EmailClient>();

        return serviceCollection;
    }
}