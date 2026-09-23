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

    /// <summary>Necha qadam = 1 coin (default 1000).</summary>
    public int StepsPerCoin { get; set; }

    /// <summary>Bir kunda qadamdan olinadigan maksimal coin (default 22).</summary>
    public int MaxDailyCoins { get; set; }
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
