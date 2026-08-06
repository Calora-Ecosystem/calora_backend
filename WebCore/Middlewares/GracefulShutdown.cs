using Microsoft.Extensions.Diagnostics.HealthChecks;
using Serilog;

namespace WebCore.Middlewares;

/// <summary>
/// Flips to true the instant SIGTERM/SIGINT arrives (before Kestrel finishes draining
/// in-flight requests), so a load balancer/reverse proxy polling /healthy stops routing
/// new traffic to this instance immediately, without cutting off requests already in flight.
/// </summary>
public static class ShutdownState
{
    private static volatile bool _isShuttingDown;
    public static bool IsShuttingDown => _isShuttingDown;
    public static void Begin() => _isShuttingDown = true;
}

public class ShutdownAwareHealthCheck : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(ShutdownState.IsShuttingDown
            ? HealthCheckResult.Unhealthy("Instance is shutting down")
            : HealthCheckResult.Healthy());
}

public static class GracefulShutdownExtensions
{
    /// <summary>
    /// Docker/Watchtower sends SIGTERM then force-kills (SIGKILL, exit 137) after their
    /// stop timeout (10s by default for both `docker stop` and Watchtower). This bounds our
    /// own drain to comfortably fit inside that default window instead of relying on the
    /// framework's 30s default, which would otherwise get cut off mid-shutdown.
    /// </summary>
    public static WebApplicationBuilder ConfigureGracefulShutdown(this WebApplicationBuilder builder)
    {
        builder.Services.Configure<HostOptions>(options =>
        {
            options.ShutdownTimeout = TimeSpan.FromSeconds(8);
        });

        builder.Services.AddHealthChecks()
            .AddCheck<ShutdownAwareHealthCheck>("shutdown");

        return builder;
    }

    public static WebApplication UseGracefulShutdown(this WebApplication app)
    {
        var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();

        lifetime.ApplicationStopping.Register(() =>
        {
            ShutdownState.Begin();
            Log.Information(
                "Received shutdown signal (PID {Pid}). Draining in-flight requests and Hangfire workers...",
                Environment.ProcessId);
        });

        lifetime.ApplicationStopped.Register(() =>
        {
            SentrySdk.Flush(TimeSpan.FromSeconds(2));
            Log.Information("Shutdown complete (PID {Pid}).", Environment.ProcessId);
        });

        return app;
    }
}
