using Microsoft.AspNetCore.Http;
using Sentry;
using Sentry.AspNetCore;
using WebCore.ConfigContracts;

namespace WebCore.Observability;

public static class SentryTraceSampler
{
    public static double? Sample(TransactionSamplingContext context, SentryOption config)
    {
        if (!config.Enabled || string.IsNullOrWhiteSpace(config.Dsn))
        {
            return 0.0;
        }

        // 1. Process HTTP Requests
        var httpContext = context.TryGetHttpContext();
        var rawPath = httpContext?.Request.Path.Value ?? context.TryGetHttpPath() ?? string.Empty;
        var method = httpContext?.Request.Method ?? context.TryGetHttpMethod() ?? string.Empty;

        // Fallback: extract HTTP method and path from TransactionContext.Name (e.g. "GET /healthy")
        if (string.IsNullOrEmpty(rawPath) && !string.IsNullOrEmpty(context.TransactionContext.Name))
        {
            var parts = context.TransactionContext.Name.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 2 && parts[0] is "GET" or "POST" or "PUT" or "DELETE" or "PATCH" or "OPTIONS" or "HEAD")
            {
                method = parts[0];
                rawPath = parts[1];
            }
            else if (context.TransactionContext.Name.StartsWith('/'))
            {
                rawPath = context.TransactionContext.Name;
            }
        }

        if (httpContext != null || !string.IsNullOrEmpty(rawPath))
        {
            // Normalize path by stripping optional "/api" prefix if present
            var normalizedPath = rawPath.StartsWith("/api/", StringComparison.OrdinalIgnoreCase)
                ? rawPath[4..]
                : (rawPath.Equals("/api", StringComparison.OrdinalIgnoreCase) ? "/" : rawPath);

            // Filter out noise endpoints (0% trace sampling)
            if (IsNoiseRequest(normalizedPath, rawPath, method))
            {
                return 0.0;
            }

            // High-priority: Billing & Payment transactions (Click, Payme, Orders, Subscriptions)
            if (normalizedPath.StartsWith("/billing", StringComparison.OrdinalIgnoreCase))
            {
                return config.BillingTracesSampleRate;
            }

            // High-priority: AI & Computer Vision (Gemini, Meal Recognition, Face Analysis)
            if (normalizedPath.StartsWith("/gemini", StringComparison.OrdinalIgnoreCase) ||
                normalizedPath.StartsWith("/face-analysis", StringComparison.OrdinalIgnoreCase) ||
                normalizedPath.StartsWith("/munosabat-ai", StringComparison.OrdinalIgnoreCase) ||
                normalizedPath.Contains("recognize", StringComparison.OrdinalIgnoreCase) ||
                normalizedPath.Contains("analyze", StringComparison.OrdinalIgnoreCase))
            {
                return config.AiTracesSampleRate;
            }

            // Important: Authentication & User Onboarding
            if (normalizedPath.StartsWith("/auth", StringComparison.OrdinalIgnoreCase))
            {
                return config.AuthTracesSampleRate;
            }

            // Standard business API requests (CRUD, GET queries, etc.)
            return config.DefaultTracesSampleRate;
        }

        // 2. Process Background Jobs (Hangfire, etc.)
        var transactionName = context.TransactionContext.Name ?? string.Empty;

        // Routine recurring background jobs (running every minute / 10-15 minutes 24/7)
        if (IsRoutineRecurringJob(transactionName))
        {
            return config.HangfireRecurringJobsSampleRate;
        }

        return config.HangfireTracesSampleRate;
    }

    private static bool IsNoiseRequest(string normalizedPath, string rawPath, string method)
    {
        // Drop CORS preflight OPTIONS requests
        if (HttpMethods.IsOptions(method))
        {
            return true;
        }

        // Drop Health check probes
        if (normalizedPath.StartsWith("/healthy", StringComparison.OrdinalIgnoreCase) ||
            normalizedPath.StartsWith("/health", StringComparison.OrdinalIgnoreCase) ||
            normalizedPath.StartsWith("/healthz", StringComparison.OrdinalIgnoreCase) ||
            rawPath.StartsWith("/healthy", StringComparison.OrdinalIgnoreCase) ||
            rawPath.StartsWith("/health", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // Drop Static files and asset downloads
        if (normalizedPath.StartsWith("/file", StringComparison.OrdinalIgnoreCase) ||
            rawPath.StartsWith("/file", StringComparison.OrdinalIgnoreCase) ||
            rawPath.EndsWith(".ico", StringComparison.OrdinalIgnoreCase) ||
            rawPath.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
            rawPath.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
            rawPath.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase) ||
            rawPath.EndsWith(".svg", StringComparison.OrdinalIgnoreCase) ||
            rawPath.EndsWith(".css", StringComparison.OrdinalIgnoreCase) ||
            rawPath.EndsWith(".js", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // Drop Swagger / OpenAPI UI & JSON schemas
        if (normalizedPath.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase) ||
            rawPath.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase) ||
            rawPath.Contains("swagger.json", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // Drop Hangfire dashboard UI and polling endpoints
        if (normalizedPath.StartsWith("/hangfire", StringComparison.OrdinalIgnoreCase) ||
            rawPath.StartsWith("/hangfire", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }

    private static bool IsRoutineRecurringJob(string transactionName)
    {
        return transactionName.Contains("reminder", StringComparison.OrdinalIgnoreCase) ||
               transactionName.Contains("notification", StringComparison.OrdinalIgnoreCase) ||
               transactionName.Contains("escalate", StringComparison.OrdinalIgnoreCase) ||
               transactionName.Contains("expiredgrant", StringComparison.OrdinalIgnoreCase) ||
               transactionName.Contains("expire_grant", StringComparison.OrdinalIgnoreCase) ||
               transactionName.Contains("index_workout", StringComparison.OrdinalIgnoreCase) ||
               transactionName.Contains("workout_computation", StringComparison.OrdinalIgnoreCase) ||
               transactionName.Contains("workoutcomputation", StringComparison.OrdinalIgnoreCase);
    }
}
