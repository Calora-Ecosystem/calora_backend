using Core.Enums;

namespace Core.Services.Ai.Contracts;

/// <summary>Dashboard: bepul AI skan limiti holati va umumiy skan statistikasi.</summary>
public record AdminScanSummaryDto
{
    /// <summary>Faol Free tarifdagi bazaviy bepul skan limiti (bonuslarsiz).</summary>
    public int FreeLimit { get; init; }

    /// <summary>Free tarifda AiScans = "unlimited".</summary>
    public bool FreeUnlimited { get; init; }

    /// <summary>Faol Free tarif bormi. Yo'q bo'lsa bepul userlar limiti 0.</summary>
    public bool FreePlanActive { get; init; }

    /// <summary>Faol Free tarifda AiScans qiymati belgilanganmi. Yo'q bo'lsa bepul userlar limiti 0.</summary>
    public bool FreeLimitConfigured { get; init; }

    public int TotalUsers { get; init; }
    public int PremiumUsers { get; init; }
    public int FreeUsers { get; init; }

    /// <summary>Kamida bitta bepul skanni ishlatgan Free userlar.</summary>
    public int FreeUsersScanned { get; init; }

    /// <summary>Bepul limiti (bonus bilan) tugagan Free userlar.</summary>
    public int ExhaustedUsers { get; init; }

    /// <summary>Do'kondan qo'shimcha skan olgan userlar va jami berilgan bonus skanlar.</summary>
    public int UsersWithBonus { get; init; }
    public long BonusGranted { get; init; }

    /// <summary>Limitdan yechilgan bepul skanlar (umr bo'yi).</summary>
    public long FreeScansUsed { get; init; }

    /// <summary>AI jurnalidagi barcha skan so'rovlari (Premium ham, xatolar ham).</summary>
    public int TotalScans { get; init; }
    public int SuccessScans { get; init; }
    public int EmptyScans { get; init; }
    public int ErrorScans { get; init; }
    public int ScansToday { get; init; }

    /// <summary>Kamida bir marta skan qilgan alohida userlar.</summary>
    public int Scanners { get; init; }

    /// <summary>Free userlar ishlatgan bepul skanlar soni bo'yicha (0 ham kiradi).</summary>
    public List<AdminScanUsageBucketDto> Distribution { get; init; } = [];
}

public record AdminScanUsageBucketDto
{
    public int Used { get; init; }
    public int Users { get; init; }
}

/// <summary>Dashboard: bitta userning bepul skan limiti va skanlari.</summary>
public record AdminScanUserDto
{
    public long UserId { get; init; }
    public string Name { get; init; } = null!;
    public string? Email { get; init; }
    public string? Phone { get; init; }
    public string? Photo { get; init; }
    public DateTime RegisteredAt { get; init; }

    /// <summary>Faol obuna tarifi (yo'q bo'lsa null — Free).</summary>
    public EnumSPlans? Plan { get; init; }

    public bool IsPremium { get; init; }
    public DateTime? PremiumEndsAt { get; init; }

    /// <summary>Premium yoki Free tarif cheksiz — limit hisoblanmaydi.</summary>
    public bool Unlimited { get; init; }

    /// <summary>Free tarifning bazaviy limiti.</summary>
    public int FreeLimit { get; init; }

    /// <summary>Do'kondan olingan qo'shimcha skanlar.</summary>
    public int BonusLimit { get; init; }

    /// <summary>Jami bepul limit (FreeLimit + BonusLimit).</summary>
    public int Limit { get; init; }

    /// <summary>Limitdan yechilgan bepul skanlar.</summary>
    public int Used { get; init; }

    public int Remaining { get; init; }

    /// <summary>AI jurnalidagi barcha skan so'rovlari (Premium davridagilari ham).</summary>
    public int TotalScans { get; init; }
    public int SuccessScans { get; init; }
    public int ErrorScans { get; init; }
    public DateTime? LastScanAt { get; init; }
}

public record AdminScanUserDetailDto
{
    public AdminScanUserDto User { get; init; } = null!;

    /// <summary>Oxirgi skan so'rovlari (yangidan eskiga).</summary>
    public List<AdminScanEventDto> Events { get; init; } = [];
}

public record AdminScanEventDto
{
    public long Id { get; init; }
    public DateTime CreatedAt { get; init; }

    /// <summary>success | empty_result | provider_error | invalid_result | parse_error.</summary>
    public string Outcome { get; init; } = null!;

    public EnumEventStatus Status { get; init; }
    public long? DurationMs { get; init; }
    public int? ItemCount { get; init; }
    public string? MimeType { get; init; }
    public string? ErrorMessage { get; init; }
}
