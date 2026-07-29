using Core.Enums;
using Core.Services.User.Contracts;

namespace Core.Services.Dashboard.Contracts;

/// <summary>
/// Bitta foydalanuvchi bo'yicha to'liq ma'lumot — ro'yxatdagi qatordan
/// bosilganda ochiladigan tafsilotlar oynasi uchun.
/// </summary>
public record GetUserDetailDto
{
    public long Id { get; set; }
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public List<string> Roles { get; set; } = [];
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public SubscriptionDto? Subscription { get; set; }
    public UserDetailExtraDto? Extra { get; set; }

    // ── Faollik ──────────────────────────────────────────────────────
    public int SignInCount { get; set; }
    public DateTime? LastSignInAt { get; set; }

    // ── Maqsad normalari ─────────────────────────────────────────────
    public List<UserNormValueDto> Norms { get; set; } = [];
}

public record UserDetailExtraDto
{
    public double Weight { get; set; }
    public double EntryWeight { get; set; }
    public double Height { get; set; }
    public double Bmi { get; set; }
    public EnumGender Gender { get; set; }
    public DateTime BirthDate { get; set; }
    public int Age { get; set; }
    public EnumPurpose Purpose { get; set; }
    public EnumPhysicalActivity? PhysicalActivity { get; set; }
    public EnumActivityLevel ActivityLevel { get; set; }
    public EnumLanguage Language { get; set; }
    public string? Photo { get; set; }
}

public record UserNormValueDto
{
    public EnumMetrics Metric { get; set; }
    public double Value { get; set; }
}
