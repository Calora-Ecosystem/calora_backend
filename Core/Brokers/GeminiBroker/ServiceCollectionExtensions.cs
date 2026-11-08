using Core.Brokers.GeminiBroker.Contracts;
using Google.GenAI;
using Google.GenAI.Types;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;

namespace Core.Brokers.GeminiBroker;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddGeminiAi(this IServiceCollection services, string configSection = "Gemini")
    {
        services
            .AddOptions<GeminiConfig>()
            .BindConfiguration(configSection)
            .ValidateDataAnnotations()
            .Validate(c => c.Resilience.TotalTimeoutSeconds >= c.Resilience.AttemptTimeoutSeconds,
                "Gemini:Resilience:TotalTimeoutSeconds must be >= AttemptTimeoutSeconds")
            .ValidateOnStart();

        services.AddSingleton((provider) =>
        {
            var option = provider.GetRequiredService<IOptions<GeminiConfig>>().Value;
            return new Client(apiKey: option.ApiKey, httpOptions: new HttpOptions
            {
                // Bitta urinish timeout'i (ms). SDK CancellationToken qabul qilmagani uchun
                // HTTP so'rovni haqiqatan uzadigan yagona joy shu.
                Timeout = option.Resilience.AttemptTimeoutSeconds * 1000,
            });
        });

        services.AddResiliencePipeline(GeminiResilience.PipelineKey, (builder, context) =>
        {
            var config = context.ServiceProvider.GetRequiredService<IOptions<GeminiConfig>>().Value.Resilience;
            var logger = context.ServiceProvider.GetRequiredService<ILoggerFactory>()
                .CreateLogger(typeof(GeminiResilience).FullName!);

            GeminiResilience.Configure(builder, config, logger);
        });

        return services;
    }
}
