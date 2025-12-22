using Core.Brokers.GeminiBroker.Contracts;
using Google.GenAI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Core.Brokers.GeminiBroker;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddGeminiAi(this IServiceCollection services, string configSection = "Gemini")
    {
        services
            .AddOptions<GeminiConfig>()
            .BindConfiguration(configSection)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton((provider) =>
        {
            var option = provider.GetRequiredService<IOptions<GeminiConfig>>();
            return new Client(apiKey: option.Value.ApiKey);
        });

        return services;
    }
}