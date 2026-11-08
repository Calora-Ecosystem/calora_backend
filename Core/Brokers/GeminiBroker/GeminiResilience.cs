using Core.Brokers.GeminiBroker.Contracts;
using Google.GenAI;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;

namespace Core.Brokers.GeminiBroker;

/// <summary>
/// Google.GenAI SDK chaqiruvlari uchun Polly pipeline.
/// Tartib (tashqaridan ichkariga): umumiy timeout → retry → circuit breaker.
/// Bitta urinish timeout'i SDK'ning <c>HttpOptions.Timeout</c> orqali beriladi, chunki
/// SDK (0.7.0) <c>CancellationToken</c> qabul qilmaydi va so'rovni faqat o'zi uza oladi.
/// </summary>
public static class GeminiResilience
{
    public const string PipelineKey = "gemini";

    /// <summary>
    /// Qayta urinishga arziydigan xatolar: 5xx, 429, tarmoq xatosi va SDK timeout'i.
    /// 400/401/403/404 kabi xatolar qayta urinilmaydi — natija o'zgarmaydi.
    /// </summary>
    public static bool IsTransient(Exception exception) => exception switch
    {
        ServerError se => se.StatusCode is 500 or 502 or 503 or 504,
        ClientError ce => ce.StatusCode == 429,
        // SDK'ning HttpClient.Timeout'i shunday ko'rinishda chiqadi.
        TaskCanceledException => true,
        HttpRequestException { StatusCode: null } => true,
        _ => false,
    };

    public static void Configure(
        ResiliencePipelineBuilder builder, GeminiResilienceConfig config, ILogger logger)
    {
        var shouldHandle = new Func<Outcome<object>, ResilienceContext, bool>((outcome, context) =>
            !context.CancellationToken.IsCancellationRequested
            && outcome.Exception is { } ex
            && IsTransient(ex));

        builder
            .AddTimeout(new TimeoutStrategyOptions
            {
                Name = "gemini-total-timeout",
                Timeout = TimeSpan.FromSeconds(config.TotalTimeoutSeconds),
            })
            .AddRetry(new RetryStrategyOptions
            {
                Name = "gemini-retry",
                MaxRetryAttempts = config.MaxRetryAttempts,
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                Delay = TimeSpan.FromMilliseconds(config.BaseDelayMs),
                ShouldHandle = args => ValueTask.FromResult(shouldHandle(args.Outcome, args.Context)),
                DelayGenerator = args =>
                {
                    if (args.Outcome.Exception is not ClientError { StatusCode: 429 })
                        return ValueTask.FromResult<TimeSpan?>(null); // standart exponential + jitter

                    // 429: kvota tiklanishiga ko'proq vaqt beramiz (3s, 6s, 12s ... + jitter).
                    var baseMs = config.RateLimitBaseDelayMs * Math.Pow(2, args.AttemptNumber);
                    var jitterMs = Random.Shared.NextDouble() * config.RateLimitBaseDelayMs;
                    return ValueTask.FromResult<TimeSpan?>(TimeSpan.FromMilliseconds(baseMs + jitterMs));
                },
                OnRetry = args =>
                {
                    var ex = args.Outcome.Exception;
                    var status = ex switch
                    {
                        ServerError se => $"{se.StatusCode} {se.Status}",
                        ClientError ce => $"{ce.StatusCode} {ce.Status}",
                        _ => ex?.GetType().Name ?? "unknown",
                    };

                    logger.LogWarning(ex,
                        "Gemini call failed ({Status}); retry {Attempt}/{MaxAttempts} in {DelayMs}ms",
                        status, args.AttemptNumber + 1, config.MaxRetryAttempts,
                        (long)args.RetryDelay.TotalMilliseconds);

                    SentrySdk.AddBreadcrumb(
                        $"Gemini retry {args.AttemptNumber + 1}/{config.MaxRetryAttempts} after {status}",
                        category: "ai.gemini", level: BreadcrumbLevel.Warning);

                    return ValueTask.CompletedTask;
                },
            })
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions
            {
                Name = "gemini-circuit-breaker",
                FailureRatio = config.CircuitFailureRatio,
                MinimumThroughput = config.CircuitMinimumThroughput,
                SamplingDuration = TimeSpan.FromSeconds(config.CircuitSamplingDurationSeconds),
                BreakDuration = TimeSpan.FromSeconds(config.CircuitBreakDurationSeconds),
                ShouldHandle = args => ValueTask.FromResult(shouldHandle(args.Outcome, args.Context)),
                OnOpened = args =>
                {
                    logger.LogError(args.Outcome.Exception,
                        "Gemini circuit OPENED for {BreakSeconds}s — AI requests will fail fast",
                        args.BreakDuration.TotalSeconds);
                    SentrySdk.CaptureMessage("Gemini circuit breaker opened", SentryLevel.Error);
                    return ValueTask.CompletedTask;
                },
                OnHalfOpened = _ =>
                {
                    logger.LogInformation("Gemini circuit HALF-OPEN — probing with next request");
                    return ValueTask.CompletedTask;
                },
                OnClosed = _ =>
                {
                    logger.LogInformation("Gemini circuit CLOSED — Gemini is healthy again");
                    return ValueTask.CompletedTask;
                },
            });
    }

    /// <summary>Pipeline'ning o'zi rad etgan holatlar (circuit ochiq yoki umumiy timeout).</summary>
    public static bool IsRejectedByPipeline(Exception exception) =>
        exception is BrokenCircuitException or TimeoutRejectedException;
}
