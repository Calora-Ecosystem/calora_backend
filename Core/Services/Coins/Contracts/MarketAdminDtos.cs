using Core.Entities.Coins.Enum;

namespace Core.Services.Coins.Contracts;

/// <summary>Dashboard: do'kon mahsuloti va uning sotuv statistikasi.</summary>
public record AdminMarketItemDto
{
    public long Id { get; init; }
    public string Title { get; init; } = null!;
    public string? Subtitle { get; init; }
    public long PriceCoins { get; init; }
    public EnumMarketCategory Category { get; init; }
    public EnumMarketRewardType RewardType { get; init; }
    public long RewardValue { get; init; }
    public bool IsPopular { get; init; }
    public bool IsActive { get; init; }
    public int SortOrder { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }

    /// <summary>Mobile do'konda ko'rinadimi (faqat faol Premium tariflar ko'rsatiladi).</summary>
    public bool VisibleInApp { get; init; }

    /// <summary>Umr bo'yi xaridlar.</summary>
    public int TotalPurchases { get; init; }

    /// <summary>Davrdagi xaridlar.</summary>
    public int Purchases { get; init; }

    /// <summary>Davrda sarflangan coinlar.</summary>
    public long CoinsSpent { get; init; }

    /// <summary>Davrda sotib olgan alohida userlar.</summary>
    public int Buyers { get; init; }

    public DateTime? LastPurchaseAt { get; init; }

    /// <summary>
    /// Faol user (kuniga ≥1 coin yig'adigan) joriy qoida bo'yicha bu narxni necha kunda yig'adi
    /// (oxirgi 30 kun qadamlari bo'yicha).
    /// </summary>
    public double? DaysToEarn { get; init; }

    /// <summary>Bir premium kun necha coin turadi (faqat PremiumDays uchun).</summary>
    public double? CoinsPerPremiumDay { get; init; }
}

public record AdminMarketSummaryDto
{
    public DateTime From { get; init; }
    public DateTime To { get; init; }

    public int Purchases { get; init; }
    public int Buyers { get; init; }
    public long CoinsSpent { get; init; }

    /// <summary>Davrda do'kon orqali berilgan premium kunlar.</summary>
    public long PremiumDaysGranted { get; init; }

    /// <summary>Davrda bir necha marta sotib olgan userlar.</summary>
    public int RepeatBuyers { get; init; }

    public int ActiveItems { get; init; }

    /// <summary>Eng arzon faol tarif narxi.</summary>
    public long? CheapestPrice { get; init; }

    /// <summary>Hozir eng arzon tarifni sotib olishga coini yetadigan userlar.</summary>
    public int CanAffordCheapest { get; init; }

    /// <summary>Barcha hamyonlardagi joriy balans.</summary>
    public long BalanceInCirculation { get; init; }

    /// <summary>Faol user joriy qoida bo'yicha bir kunda o'rtacha yig'adigan coin (oxirgi 30 kun).</summary>
    public double AvgCoinsPerEarningDay { get; init; }

    public int StepsPerCoin { get; init; }
    public int MaxDailyCoins { get; init; }

    /// <summary>Kunma-kun xaridlar (davr, ko'pi bilan 366 kun).</summary>
    public List<AdminMarketDayDto> Days { get; init; } = [];
}

public record AdminMarketDayDto
{
    public DateTime Date { get; init; }
    public int Purchases { get; init; }
    public long CoinsSpent { get; init; }
}

public record AdminMarketPurchaseDto
{
    public long Id { get; init; }
    public DateTime CreatedAt { get; init; }

    public long UserId { get; init; }
    public string? Name { get; init; }
    public string? Email { get; init; }
    public string? Phone { get; init; }
    public string? Photo { get; init; }

    public long MarketItemId { get; init; }
    public string Title { get; init; } = null!;
    public long PriceCoins { get; init; }
    public EnumMarketRewardType RewardType { get; init; }
    public long RewardValue { get; init; }
    public string? Code { get; init; }
}
