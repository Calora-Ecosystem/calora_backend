namespace WebCore.ConfigContracts;

public class SentryOption
{
    public const string SectionName = "Sentry";

    public string? Dsn { get; set; }
    public bool Enabled { get; set; } = true;
    public bool Debug { get; set; } = false;

    /// <summary>
    /// Default sampling rate for regular API endpoints (e.g. 0.05 = 5%).
    /// </summary>
    public double DefaultTracesSampleRate { get; set; } = 0.05;

    /// <summary>
    /// Sampling rate for critical billing and payment endpoints (e.g. 0.5 = 50%).
    /// </summary>
    public double BillingTracesSampleRate { get; set; } = 0.5;

    /// <summary>
    /// Sampling rate for AI endpoints (Gemini, Face Analysis, Food Recognition) (e.g. 0.5 = 50%).
    /// </summary>
    public double AiTracesSampleRate { get; set; } = 0.5;

    /// <summary>
    /// Sampling rate for authentication endpoints (e.g. 0.2 = 20%).
    /// </summary>
    public double AuthTracesSampleRate { get; set; } = 0.2;

    /// <summary>
    /// Sampling rate for general Hangfire background jobs (e.g. 0.02 = 2%).
    /// </summary>
    public double HangfireTracesSampleRate { get; set; } = 0.02;

    /// <summary>
    /// Sampling rate for routine recurring Hangfire heartbeat/maintenance jobs (e.g. 0.0 = 0%).
    /// </summary>
    public double HangfireRecurringJobsSampleRate { get; set; } = 0.0;
}
