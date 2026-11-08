using System.ComponentModel.DataAnnotations;

namespace Core.Brokers.GeminiBroker.Contracts;

public class GeminiConfig
{
    [Required] public string ApiKey { get; set; } = null!;

    public GeminiResilienceConfig Resilience { get; set; } = new();
}

/// <summary>
/// Gemini chaqiruvlari uchun Polly sozlamalari ("Gemini:Resilience" bo'limi, hammasi ixtiyoriy).
/// </summary>
public class GeminiResilienceConfig
{
    /// <summary>Bitta urinish (HTTP so'rov) uchun timeout — SDK darajasida qo'llanadi.</summary>
    [Range(1, 300)] public int AttemptTimeoutSeconds { get; set; } = 25;

    /// <summary>Barcha urinishlar va kutishlar uchun umumiy muddat.</summary>
    [Range(1, 600)] public int TotalTimeoutSeconds { get; set; } = 60;

    [Range(0, 5)] public int MaxRetryAttempts { get; set; } = 2;

    [Range(0, 30_000)] public int BaseDelayMs { get; set; } = 1_000;

    /// <summary>429 (RESOURCE_EXHAUSTED) uchun bazaviy kutish — odatdagidan uzoqroq.</summary>
    [Range(0, 60_000)] public int RateLimitBaseDelayMs { get; set; } = 3_000;

    [Range(0.01, 1.0)] public double CircuitFailureRatio { get; set; } = 0.5;

    [Range(2, 1_000)] public int CircuitMinimumThroughput { get; set; } = 10;

    [Range(1, 600)] public int CircuitSamplingDurationSeconds { get; set; } = 60;

    [Range(1, 600)] public int CircuitBreakDurationSeconds { get; set; } = 30;
}
