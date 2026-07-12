using Core.Enums;

namespace Core.Services.Dashboard.Contracts;

/// <summary>
/// Foydalanuvchilar bo'yicha to'liq statistika: ro'yxatdan o'tish,
/// faollik (DAU/WAU/MAU) va obuna (premium/bepul) kesimlari.
/// </summary>
public record GetUserStatisticsDto
{
    // ── Registratsiya (jami va yangi) ────────────────────────────────
    public int TotalUsers { get; set; }
    public int NewToday { get; set; }
    public int NewThisWeek { get; set; }
    public int NewThisMonth { get; set; }

    // O'sish foizi (avvalgi bir xil davrga nisbatan)
    public double NewTodayGrows { get; set; }
    public double NewThisWeekGrows { get; set; }
    public double NewThisMonthGrows { get; set; }

    // ── Faollik (SignLog asosida — ilovadan foydalanganlar) ──────────
    public int ActiveToday { get; set; }   // DAU
    public int ActiveThisWeek { get; set; } // WAU
    public int ActiveThisMonth { get; set; } // MAU

    // ── Obuna kesimi ─────────────────────────────────────────────────
    public int PremiumUsers { get; set; } // faol Premium yoki Pro obuna
    public int FreeUsers { get; set; }     // obunasi yo'q / faol emas
    public List<PlanBreakdownDto> PlanBreakdown { get; set; } = [];

    // ── Trendlar ─────────────────────────────────────────────────────
    // Oxirgi 30 kunlik kunlik ro'yxatdan o'tishlar
    public List<DailyCountDto> DailyRegistrations { get; set; } = [];
    // Oxirgi 30 kunlik kunlik faol foydalanuvchilar
    public List<DailyCountDto> DailyActiveUsers { get; set; } = [];
    // Joriy yilning oylik ro'yxatdan o'tishlari (1-12)
    public Dictionary<int, int> MonthlyRegistrations { get; set; } = new();
}

/// <summary>
/// Tanlangan sana oralig'i bo'yicha statistika. Bir kun uchun from == to
/// (o'sha kun 00:00 dan keyingi kun 00:00 gacha) yuboriladi.
/// </summary>
public record GetUserStatisticsRangeDto
{
    public DateTime From { get; set; }
    public DateTime To { get; set; }

    public int Registered { get; set; }   // oraliqda ro'yxatdan o'tganlar
    public int ActiveUsers { get; set; }   // oraliqda ilovaga kirgan noyob foydalanuvchilar
    public int SignInCount { get; set; }   // oraliqdagi jami kirishlar soni
    public int NewPremium { get; set; }    // oraliqda boshlangan Premium/Pro obunalar

    public List<DailyCountDto> DailyRegistrations { get; set; } = [];
    public List<DailyCountDto> DailyActiveUsers { get; set; } = [];
    public List<DailyCountDto> DailyPremium { get; set; } = [];
}

public record PlanBreakdownDto
{
    public EnumSPlans Plan { get; set; }
    public int Count { get; set; }
}

public record DailyCountDto
{
    public DateTime Date { get; set; }
    public int Count { get; set; }
}
