using Core.Enums;

namespace Core.Services.Dashboard.Contracts;

/// <summary>
/// Berilgan Source (masalan "ai.food_recognition") bo'yicha umumiy hodisa jurnali
/// tahlili: nechta hodisa, qanday holatda tugagan, xato bo'lsa qaysi turdagi xato
/// eng ko'p uchragan va kunlik trend.
/// </summary>
public record GetEventLogSummaryDto
{
    public string Source { get; set; } = null!;
    public DateTime From { get; set; }
    public DateTime To { get; set; }

    public int TotalCount { get; set; }
    public int SuccessCount { get; set; }
    public int WarningCount { get; set; }
    public int ErrorCount { get; set; }

    /// <summary>Xato foizi (ErrorCount / TotalCount * 100), hodisa bo'lmasa 0.</summary>
    public double ErrorRatePercent { get; set; }

    public double? AvgDurationMs { get; set; }
    public long? MaxDurationMs { get; set; }

    /// <summary>Aniq natija kodi bo'yicha taqsimot (masalan "success", "empty_result", "provider_error").</summary>
    public List<EventOutcomeCountDto> OutcomeBreakdown { get; set; } = [];

    /// <summary>Eng ko'p uchragan xato turlari (ErrorType bo'yicha), eng ko'pdan kamga.</summary>
    public List<EventErrorCountDto> TopErrors { get; set; } = [];

    public List<DailyEventCountDto> DailyTrend { get; set; } = [];
}

public record EventOutcomeCountDto
{
    public string Outcome { get; set; } = null!;
    public EnumEventStatus Status { get; set; }
    public int Count { get; set; }
}

public record EventErrorCountDto
{
    public string ErrorType { get; set; } = null!;
    public int Count { get; set; }

    /// <summary>Shu xato turidan eng so'nggi namunaviy xabar (diagnostika uchun).</summary>
    public string? SampleMessage { get; set; }
}

public record DailyEventCountDto
{
    public DateTime Date { get; set; }
    public int Total { get; set; }
    public int Success { get; set; }
    public int Warning { get; set; }
    public int Error { get; set; }
}
