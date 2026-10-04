using Sentry.AspNetCore;
using WebCore.ConfigContracts;

namespace WebCore.Observability;

public static class SentryConfigurationExtensions
{
    private const string DefaultDsn = "https://eb9278a9d051a5569640077a8fe6eb23@o4510963491536896.ingest.us.sentry.io/4511376801923072";

    public static WebApplicationBuilder ConfigureSentry(this WebApplicationBuilder builder)
    {
        var sentryOption = builder.Configuration
            .GetSection(SentryOption.SectionName)
            .Get<SentryOption>() ?? new SentryOption();

        // Fallback to default DSN if not explicitly specified
        if (string.IsNullOrWhiteSpace(sentryOption.Dsn))
        {
            sentryOption.Dsn = DefaultDsn;
        }

        // If explicitly disabled or DSN is empty, do not initialize Sentry
        if (!sentryOption.Enabled || string.IsNullOrWhiteSpace(sentryOption.Dsn))
        {
            return builder;
        }

        builder.WebHost.UseSentry((SentryAspNetCoreOptions options) =>
        {
            options.Dsn = sentryOption.Dsn;
            options.Environment = builder.Environment.EnvironmentName;
            options.Debug = sentryOption.Debug;

            // Dynamic sampling: filters out noise and prioritizes critical business flows
            options.TracesSampler = context => SentryTraceSampler.Sample(context, sentryOption);

            // Log configuration: breadcrumbs capture info, but log messages don't burn event quota
            options.MinimumEventLevel = LogLevel.Error;
            options.MinimumBreadcrumbLevel = LogLevel.Information;
            options.EnableLogs = false;

            // Disable metrics to avoid quota depletion
            options.EnableMetrics = false;

            // Filter out expected client network cancellations
            options.SetBeforeSend((sentryEvent, _) =>
            {
                if (sentryEvent.Exception is OperationCanceledException or TaskCanceledException)
                {
                    return null;
                }

                return sentryEvent;
            });
        });

        return builder;
    }
}
