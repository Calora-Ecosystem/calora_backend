using System.ComponentModel.DataAnnotations;
using Core.Entities.Coins.Enum;
using Core.Services.User.Contracts;

namespace Core.Services.Coins.Contracts;

public class WalletDto
{
    public long Balance { get; set; }
    public long TotalEarned { get; set; }
    public long TotalSpent { get; set; }

    /// <summary>Bugungi qadamlar uchun berilgan coin.</summary>
    public long TodayCoins { get; set; }

    /// <summary>Necha qadam = 1 coin — bugun amal qilayotgan qoida (dashboard'dan boshqariladi).</summary>
    public int StepsPerCoin { get; set; }

    /// <summary>Bir kunda qadamdan olinadigan maksimal coin — bugungi qoida.</summary>
    public int MaxDailyCoins { get; set; }

    /// <summary>Kelajakda kuchga kiradigan qoida (e'lon qilish uchun); yo'q bo'lsa null.</summary>
    public CoinRuleBriefDto? NextRule { get; set; }
}

public class CoinTransactionDto
{
    public long Id { get; set; }

    /// <summary>Mobile lokalizatsiya kaliti.</summary>
    public string Title { get; set; } = null!;

    /// <summary>Ishorali: musbat — kirim, manfiy — chiqim.</summary>
    public long Amount { get; set; }

    public EnumCoinTxType Type { get; set; }
    public DateTime CreatedAt { get; set; }

    /// <summary>Qadam coini uchun: coin qaysi kunning qadamlari uchun berilgan.
    /// <see cref="CreatedAt"/> — coin yozilgan vaqt, u kechroq bo'lishi mumkin.</summary>
    public DateTime? StepDate { get; set; }

    /// <summary>Qadam coini uchun: o'sha kungi qadamlar soni.</summary>
    public long? Steps { get; set; }
}

public class MarketItemDto
{
    public long Id { get; set; }
    public string Title { get; set; } = null!;
    public string? Subtitle { get; set; }
    public long PriceCoins { get; set; }
    public EnumMarketCategory Category { get; set; }
    public EnumMarketRewardType RewardType { get; set; }
    public long RewardValue { get; set; }
    public bool IsPopular { get; set; }
    public bool IsActive { get; set; }
    public int SortOrder { get; set; }
}

public class CreateOrUpdateMarketItemDto
{
    public long? Id { get; set; }
    [Required, MaxLength(100)] public string Title { get; set; } = null!;
    [MaxLength(100)] public string? Subtitle { get; set; }
    [Range(1, long.MaxValue)] public long PriceCoins { get; set; }
    [Required] public EnumMarketCategory Category { get; set; }
    [Required] public EnumMarketRewardType RewardType { get; set; }
    [Range(0, long.MaxValue)] public long RewardValue { get; set; }
    public bool IsPopular { get; set; }
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
}

public class MarketPurchaseDto
{
    public long Id { get; set; }
    public long MarketItemId { get; set; }
    public string Title { get; set; } = null!;
    public long PriceCoins { get; set; }
    public EnumMarketRewardType RewardType { get; set; }
    public long RewardValue { get; set; }

    /// <summary>Coupon/Voucher kodi (Coupon kodi obuna to'lovida <c>billing/coupons/check</c> orqali ishlatiladi).</summary>
    public string? Code { get; set; }

    public DateTime CreatedAt { get; set; }
}

public class PurchaseResultDto
{
    public MarketPurchaseDto Purchase { get; set; } = null!;
    public WalletDto Wallet { get; set; } = null!;

    /// <summary>
    /// Premium berilganda true — plan JWT ichida, shuning uchun mobile tokenni
    /// yangilashi (<c>auth/refresh-token</c>) kerak.
    /// </summary>
    public bool RequiresTokenRefresh { get; set; }
}

public record GetCoinStatDto
{
    public UserDto User { get; init; } = null!;

    /// <summary>Davr ichida ishlab topilgan coinlar.</summary>
    public long Sum { get; init; }

    public int Index { get; init; }
}

#region Admin (dashboard)

/// <summary>Dashboard coin reytingi qatori: davrda ishlab topilgan coinlar va hamyon holati.</summary>
public record AdminCoinRankingDto
{
    /// <summary>Davr bo'yicha reytingdagi o'rni (qidiruvdan qat'i nazar — umumiy reyting).</summary>
    public int Rank { get; init; }

    public long UserId { get; init; }
    public string Name { get; init; } = null!;
    public string? Email { get; init; }
    public string? Phone { get; init; }
    public string? Photo { get; init; }

    /// <summary>Davrda ishlab topilgan coinlar (<see cref="StepCoins"/> + <see cref="BonusCoins"/>).</summary>
    public long Earned { get; init; }

    public long StepCoins { get; init; }

    /// <summary>Qadamdan boshqa kirimlar (referral, admin va h.k.).</summary>
    public long BonusCoins { get; init; }

    /// <summary>Davrda coin yig'ilgan kunlar soni.</summary>
    public int ActiveDays { get; init; }

    /// <summary>Kunlik limitga (<c>MaxDailyCoins</c>) yetgan kunlar soni.</summary>
    public int MaxedDays { get; init; }

    public long Balance { get; init; }
    public long TotalEarned { get; init; }
    public long TotalSpent { get; init; }

    /// <summary>Oxirgi marta coin yozilgan/oshgan vaqt.</summary>
    public DateTime? LastEarnedAt { get; init; }
}

public record AdminCoinSummaryDto
{
    public DateTime From { get; init; }
    public DateTime To { get; init; }

    /// <summary>Davrda kamida 1 coin yig'gan userlar.</summary>
    public int Participants { get; init; }

    public long Earned { get; init; }
    public long StepCoins { get; init; }
    public long BonusCoins { get; init; }

    /// <summary>Davrda marketplace'da sarflangan coinlar.</summary>
    public long Spent { get; init; }

    public double AvgPerParticipant { get; init; }

    /// <summary>Barcha hamyonlardagi joriy balans yig'indisi.</summary>
    public long BalanceInCirculation { get; init; }

    public int StepsPerCoin { get; init; }
    public int MaxDailyCoins { get; init; }
}

public record AdminUserCoinsDto
{
    public long UserId { get; init; }
    public string Name { get; init; } = null!;
    public string? Email { get; init; }
    public string? Phone { get; init; }
    public string? Photo { get; init; }
    public DateTime RegisteredAt { get; init; }

    public long Balance { get; init; }
    public long TotalEarned { get; init; }
    public long TotalSpent { get; init; }

    /// <summary>Bugungi qadamlar uchun berilgan coin.</summary>
    public long TodayCoins { get; init; }

    public DateTime From { get; init; }
    public DateTime To { get; init; }

    /// <summary>Davr reytingidagi o'rni; davrda coin yig'magan bo'lsa null.</summary>
    public int? Rank { get; init; }

    /// <summary>Davr reytingidagi jami ishtirokchilar.</summary>
    public int Participants { get; init; }

    public long Earned { get; init; }
    public long StepCoins { get; init; }
    public long BonusCoins { get; init; }
    public long Spent { get; init; }
    public int ActiveDays { get; init; }
    public int MaxedDays { get; init; }
    public long TotalSteps { get; init; }
    public AdminCoinDayDto? BestDay { get; init; }

    public int StepsPerCoin { get; init; }
    public int MaxDailyCoins { get; init; }

    /// <summary>Davrning har bir kuni eskidan yangiga, coinsiz kunlar ham.</summary>
    public List<AdminCoinDayDto> Days { get; init; } = [];
}

public record AdminCoinDayDto
{
    public DateTime Date { get; init; }

    /// <summary>O'sha kungi qadamlar (<c>user_dailies</c>).</summary>
    public long Steps { get; init; }

    /// <summary>O'sha kun qadamlari uchun coin (qachon yozilganidan qat'i nazar).</summary>
    public long StepCoins { get; init; }

    /// <summary>O'sha kuni yozilgan boshqa kirimlar (referral, admin).</summary>
    public long BonusCoins { get; init; }

    /// <summary>O'sha kuni sarflangan coinlar (musbat son).</summary>
    public long Spent { get; init; }

    /// <summary>O'sha kunda amal qilgan qoida: necha qadam = 1 coin.</summary>
    public int StepsPerCoin { get; init; }

    /// <summary>O'sha kunda amal qilgan kunlik limit.</summary>
    public int MaxDailyCoins { get; init; }

    public long Earned => StepCoins + BonusCoins;
}

#endregion
