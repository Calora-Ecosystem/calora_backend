using Core.Enums;

namespace Core.Services.Dashboard.Contracts;

/// <summary>
/// Auditoriya tahlili: foydalanuvchilar qaysi soatda/hafta kunida ko'p ro'yxatdan
/// o'tishi, jinsi, yosh guruhi va boshqa profil kesimlari bo'yicha taqsimot.
/// </summary>
public record GetAudienceAnalyticsDto
{
    /// <summary>Umumiy hisobga olingan foydalanuvchilar soni.</summary>
    public int TotalUsers { get; set; }

    /// <summary>Profil (extra) ma'lumoti to'ldirilgan foydalanuvchilar soni.</summary>
    public int ProfiledUsers { get; set; }

    // ── Vaqt kesimlari (registratsiya CreatedAt bo'yicha) ────────────
    /// <summary>Har bir soat uchun (0–23) ro'yxatdan o'tishlar soni.</summary>
    public List<HourCountDto> HourlyRegistrations { get; set; } = [];

    /// <summary>Hafta kunlari bo'yicha (0=Yakshanba … 6=Shanba) ro'yxatdan o'tishlar.</summary>
    public List<WeekdayCountDto> WeekdayRegistrations { get; set; } = [];

    /// <summary>Eng ko'p ro'yxatdan o'tish soati (0–23), yo'q bo'lsa null.</summary>
    public int? PeakHour { get; set; }

    // ── Demografik kesimlar (UserExtra bo'yicha) ─────────────────────
    public List<GenderCountDto> GenderBreakdown { get; set; } = [];
    public List<AgeGroupCountDto> AgeGroups { get; set; } = [];
    public List<EnumCountDto> PurposeBreakdown { get; set; } = [];
    public List<EnumCountDto> ActivityLevelBreakdown { get; set; } = [];
    public List<EnumCountDto> LanguageBreakdown { get; set; } = [];
}

public record HourCountDto
{
    public int Hour { get; set; }
    public int Count { get; set; }
}

public record WeekdayCountDto
{
    public int Weekday { get; set; }
    public int Count { get; set; }
}

public record GenderCountDto
{
    public EnumGender Gender { get; set; }
    public int Count { get; set; }
}

public record AgeGroupCountDto
{
    public string Group { get; set; } = null!;
    public int Count { get; set; }
}

/// <summary>Enum qiymatining raqamli kodi va nomi bo'yicha sanoq (frontend yorliqlash uchun).</summary>
public record EnumCountDto
{
    public int Value { get; set; }
    public string Name { get; set; } = null!;
    public int Count { get; set; }
}
