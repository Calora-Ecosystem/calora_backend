using Core.Services.Billing.Payme.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Core.Services.Billing.Payme.Extensions;

public static class ConfigurationExtensions
{
    public static IServiceCollection AddPaymeConfig(this IServiceCollection serviceCollection)
    {
        serviceCollection
            .AddOptions<PaymeConfig>()
            .ValidateDataAnnotations()
            .ValidateOnStart();

        return serviceCollection;
    }
}