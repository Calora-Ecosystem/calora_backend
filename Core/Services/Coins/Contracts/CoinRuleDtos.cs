using System.ComponentModel.DataAnnotations;

namespace Core.Services.Coins.Contracts;

/// <summary>Bir kun uchun amal qiladigan qadam → coin qoidasi.</summary>
public record CoinRuleSnapshot(long? Id, int StepsPerCoin, int MaxDailyCoins, DateTime EffectiveFrom);

/// <summary>
/// Barcha qoidalar (sana bo'yicha o'sib boradi). <see cref="For"/> — o'sha kunda amal qilgan qoida;
/// birinchi qoidadan oldingi kunlar uchun <c>CoinConfig</c> qiymatlari (<see cref="Base"/>).
/// </summary>
public sealed class CoinRuleSet(CoinRuleSnapshot @base, IReadOnlyList<CoinRuleSnapshot> rules)
{
    public CoinRuleSnapshot Base { get; } = @base;
    public IReadOnlyList<CoinRuleSnapshot> Rules { get; } = rules;

    public CoinRuleSnapshot For(DateTime day)
    {
        var date = day.Date;
        var match = Base;
        foreach (var rule in Rules)
        {
            if (rule.EffectiveFrom > date) break;
            match = rule;
        }

        return match;
    }

    public CoinRuleSnapshot Today => For(DateTime.Now.Date);

    /// <summary>Bugundan keyin kuchga kiradigan eng yaqin qoida.</summary>
    public CoinRuleSnapshot? Next => Rules.FirstOrDefault(x => x.EffectiveFrom > DateTime.Now.Date);

    /// <summary>Kunlik qadam uchun coin: <c>min(qadam / StepsPerCoin, MaxDailyCoins)</c>.</summary>
    public long CoinsFor(DateTime day, double steps)
    {
        var rule = For(day);
        return rule.StepsPerCoin <= 0 ? 0 : Math.Min((long)(steps / rule.StepsPerCoin), rule.MaxDailyCoins);
    }
}

/// <summary>Mobile hamyonida e'lon qilinadigan keyingi qoida.</summary>
public record CoinRuleBriefDto
{
    public int StepsPerCoin { get; init; }
    public int MaxDailyCoins { get; init; }
    public DateTime EffectiveFrom { get; init; }
}

public record AdminCoinRuleDto
{
    /// <summary>null — <c>appsettings</c>dagi boshlang'ich qoida (DB'da yozuv yo'q).</summary>
    public long? Id { get; init; }

    public int StepsPerCoin { get; init; }
    public int MaxDailyCoins { get; init; }
    public DateTime EffectiveFrom { get; init; }

    /// <summary>Qoida tugagan kun (keyingi qoidadan bir kun oldin); hali amal qilsa null.</summary>
    public DateTime? EffectiveTo { get; init; }

    public string? Note { get; init; }
    public string? CreatedBy { get; init; }
    public DateTime? CreatedAt { get; init; }

    /// <summary><c>Past</c>, <c>Current</c> yoki <c>Upcoming</c>.</summary>
    public string Status { get; init; } = null!;

    /// <summary>Bugungi va kelajakdagi qoidalarni o'zgartirish/o'chirish mumkin, o'tganlarini emas.</summary>
    public bool Editable { get; init; }

    /// <summary>Boshlang'ich (appsettings) qoida.</summary>
    public bool IsDefault { get; init; }
}

public record AdminCoinRulesDto
{
    public AdminCoinRuleDto Current { get; init; } = null!;
    public AdminCoinRuleDto? Next { get; init; }

    /// <summary>Yangidan eskiga.</summary>
    public List<AdminCoinRuleDto> History { get; init; } = [];

    /// <summary>Coin yig'ish ishga tushgan kun.</summary>
    public DateTime CoinsEarnStartDate { get; init; }

    public DateTime Today { get; init; }
}

public class SaveCoinRuleDto
{
    [Range(1, 100_000)] public int StepsPerCoin { get; set; }
    [Range(1, 1_000)] public int MaxDailyCoins { get; set; }

    /// <summary>Kuchga kirish kuni: bugun yoki kelajak. Berilmasa — bugun. Shu kunga qoida bo'lsa yangilanadi.</summary>
    public DateTime? EffectiveFrom { get; set; }

    [MaxLength(300)] public string? Note { get; set; }
}

/// <summary>
/// "Agar qoidani o'zgartirsak nima bo'ladi" — oxirgi kunlardagi haqiqiy qadamlar bo'yicha
/// joriy va taklif qilingan qoida bilan beriladigan coinlar.
/// </summary>
public record CoinRulePreviewDto
{
    public DateTime From { get; init; }
    public DateTime To { get; init; }
    public int Days { get; init; }

    /// <summary>Davrda kamida 1 kun qadam yozgan userlar.</summary>
    public int Walkers { get; init; }

    /// <summary>User-kunlar soni (qadam yozilgan).</summary>
    public int WalkerDays { get; init; }

    /// <summary>Bir user-kunga o'rtacha qadam.</summary>
    public double AvgDailySteps { get; init; }

    /// <summary>Bir user-kunga mediana qadam.</summary>
    public double MedianDailySteps { get; init; }

    public CoinRuleImpactDto Current { get; init; } = null!;
    public CoinRuleImpactDto Proposed { get; init; } = null!;
}

public record CoinRuleImpactDto
{
    public int StepsPerCoin { get; init; }
    public int MaxDailyCoins { get; init; }

    /// <summary>Davrda jami beriladigan coin.</summary>
    public long TotalCoins { get; init; }

    /// <summary>Bir kunda o'rtacha beriladigan coin (barcha userlar).</summary>
    public double CoinsPerDay { get; init; }

    /// <summary>Bir user-kunga o'rtacha coin.</summary>
    public double AvgCoinsPerWalkerDay { get; init; }

    /// <summary>Kunlik limitga yetgan user-kunlar ulushi (0..1).</summary>
    public double CappedShare { get; init; }

    /// <summary>Kamida 1 coin olgan user-kunlar ulushi (0..1).</summary>
    public double EarningShare { get; init; }

    /// <summary>Faol (≥1 coin) user har kuni o'rtacha yig'adigan coin — tarif narxini "necha kunda" ga aylantirish uchun.</summary>
    public double AvgCoinsPerEarningDay { get; init; }
}

/// <summary>Dashboard: coin hisoblash kuni, hozirgi coinlar holati va reset tarixi.</summary>
public record AdminCoinEarnStartDto
{
    /// <summary>Shu kundan boshlab qadam coin beradi (kelajak bo'lsa — o'sha kungacha coin yozilmaydi).</summary>
    public DateTime EarnStartDate { get; init; }

    /// <summary>Dashboard'da hali o'rnatilmagan — appsettings qiymati.</summary>
    public bool IsDefault { get; init; }

    public string? UpdatedBy { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public DateTime Today { get; init; }

    /// <summary>Balansi yoki tarixi bor hamyonlar.</summary>
    public int WalletsWithCoins { get; init; }

    /// <summary>Barcha hamyonlardagi joriy balans.</summary>
    public long Balance { get; init; }

    /// <summary>Barcha hamyonlarning umr bo'yi ishlab topgan coinlari.</summary>
    public long Earned { get; init; }

    public int Transactions { get; init; }

    /// <summary>Coin yozilgan eng erta qadam kuni.</summary>
    public DateTime? EarliestStepDay { get; init; }

    /// <summary>Hisoblash kunidan oldingi kunlar uchun yozilgan qadam coinlari (reset qilinmagan bo'lsa qoladi).</summary>
    public long StepCoinsBeforeStart { get; init; }

    /// <summary>Yangidan eskiga.</summary>
    public List<AdminCoinResetDto> Resets { get; init; } = [];
}

public record AdminCoinResetDto
{
    public long Id { get; init; }
    public DateTime CreatedAt { get; init; }
    public string? CreatedBy { get; init; }
    public DateTime EarnStartDate { get; init; }
    public int UsersAffected { get; init; }
    public long BalanceRemoved { get; init; }
    public long EarnedRemoved { get; init; }
    public int TransactionsRemoved { get; init; }
}

public class SaveCoinEarnStartDto
{
    /// <summary>Coin hisoblash boshlanadigan kun (o'tgan kun ham bo'lishi mumkin — o'sha kundan qayta hisoblanadi).</summary>
    [Required] public DateTime EarnStartDate { get; set; }

    /// <summary>
    /// true — barcha userlarning coin balansi va coin tarixi o'chiriladi (hamyonlar 0). Do'kon xaridlari va
    /// berilgan Premium qoladi. Keyin coin faqat <see cref="EarnStartDate"/>dan boshlab qayta yig'iladi.
    /// </summary>
    public bool ResetCoins { get; set; }
}
