using BRB.Core.Common.Models;
using Core.Enums;

namespace Core.Services.Reports.Contracts;

/// <summary>
/// Haftalik hisobot (Du–Ya). Mobile dushanba kuni birinchi kirishda story ko'rinishida ko'rsatadi
/// va oxirgi slaydni rasm qilib ulashadi.
/// </summary>
public record WeeklyReportDto
{
    public string Name { get; set; } = null!;

    /// <summary>Hafta boshi (dushanba) va oxiri (yakshanba), sanalar.</summary>
    public DateTime WeekStart { get; set; }
    public DateTime WeekEnd { get; set; }

    /// <summary>Hafta davomida ovqat yozilgan kunlar soni. 3 dan kam bo'lsa mobile qisqa variantni ko'rsatadi.</summary>
    public int LoggedDays { get; set; }

    /// <summary>Kaloriya normasi ichida bo'lgan kunlar (ovqat yozilgan va normaning 75%–110% oralig'ida).</summary>
    public int DaysInNorm { get; set; }

    public WeeklyNormsDto Norms { get; set; } = null!;

    /// <summary>Doim 7 ta element, dushanbadan boshlab.</summary>
    public List<WeeklyDayDto> Days { get; set; } = [];

    public WeeklyTotalsDto Totals { get; set; } = null!;
    public WeeklyTotalsDto Averages { get; set; } = null!;

    /// <summary>Ovqat mahallari bo'yicha haftalik kkal.</summary>
    public Dictionary<EnumMenu, double> KcalByMenu { get; set; } = [];

    public WeeklyTopFoodDto? TopFood { get; set; }

    /// <summary>Eng ko'p kkal yeyilgan kun (ovqat yozilmagan bo'lsa null).</summary>
    public DateTime? HeaviestDay { get; set; }

    /// <summary>Eng ko'p qadam bosilgan kun (qadam bo'lmasa null).</summary>
    public DateTime? MostActiveDay { get; set; }

    /// <summary>Qadamdan ishlangan coinlar.</summary>
    public long CoinsEarned { get; set; }

    /// <summary>Hafta oxirigacha ketma-ket ovqat yozilgan kunlar.</summary>
    public int Streak { get; set; }

    /// <summary>O'tgan hafta bilan solishtirish, foizda. O'tgan haftada data bo'lmasa null.</summary>
    public double? KcalAvgChangePercent { get; set; }
    public double? StepsChangePercent { get; set; }

    /// <summary>Mobile lokalizatsiya kalitlari: <c>perfect_week</c>, <c>consistent</c>, <c>step_master</c>, <c>protein_pro</c>, <c>hydrated</c>.</summary>
    public List<string> Badges { get; set; } = [];
}

public record WeeklyNormsDto
{
    public double Kcal { get; set; }
    public double Protein { get; set; }
    public double Fat { get; set; }
    public double Carb { get; set; }
    public double Water { get; set; }
    public double Step { get; set; }
}

public record WeeklyDayDto
{
    public DateTime Date { get; set; }
    public double Kcal { get; set; }
    public double Protein { get; set; }
    public double Fat { get; set; }
    public double Carb { get; set; }
    public double Water { get; set; }
    public double Steps { get; set; }
    public int MealCount { get; set; }
    public bool InNorm { get; set; }
}

public record WeeklyTotalsDto
{
    public double Kcal { get; set; }
    public double Protein { get; set; }
    public double Fat { get; set; }
    public double Carb { get; set; }
    public double Water { get; set; }
    public double Steps { get; set; }
    public int MealCount { get; set; }
}

public record WeeklyTopFoodDto
{
    public long Id { get; set; }
    public MultiLanguageField Name { get; set; } = null!;
    public string? CoverUrl { get; set; }
    public int Count { get; set; }
}
