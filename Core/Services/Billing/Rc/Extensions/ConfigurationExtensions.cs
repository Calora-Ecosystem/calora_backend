using Core.Services.Billing.Rc.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Core.Services.Billing.Rc.Extensions;

public static class ConfigurationExtensions
{
    public static IServiceCollection AddRcConfig(this IServiceCollection serviceCollection)
    {
        serviceCollection
            .AddOptions<RcConfig>()
            .BindConfiguration("Rc")
            .ValidateDataAnnotations()
            .ValidateOnStart();

        serviceCollection
            .AddScoped<RcService>();

        return serviceCollection;
    }
}