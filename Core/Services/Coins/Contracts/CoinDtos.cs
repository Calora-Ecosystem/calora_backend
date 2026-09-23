using System.ComponentModel.DataAnnotations;
using Core.Entities.Coins.Enum;
using Core.Services.User.Contracts;

namespace Core.Services.Coins.Contracts;

public class WalletDto
{
    public long Balance { get; set; }
    public long TotalEarned { get; set; }
    public long TotalSpent { get; set; }

    /// <summary>Coinga almashtirish mumkin bo'lgan Calora (yig'ilgan − almashtirilgan).</summary>
    public long AvailableCalora { get; set; }

    /// <summary>Qadamlardan jami yig'ilgan Calora.</summary>
    public long EarnedCalora { get; set; }

    public long CaloraExchanged { get; set; }
    public int CaloraPerCoin { get; set; }

    /// <summary>Hozir almashtirsa necha coin chiqadi.</summary>
    public long MaxExchangeableCoins { get; set; }
}

public class CoinTransactionDto
{
    public long Id { get; set; }

    /// <summary>Mobile lokalizatsiya kaliti.</summary>
    public string Title { get; set; } = null!;

    /// <summary>Ishorali: musbat — kirim, manfiy — chiqim.</summary>
    public long Amount { get; set; }

    public EnumCoinTxType Type { get; set; }
    public long? Calora { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ExchangeCaloraDto
{
    /// <summary>Almashtiriladigan Calora; faqat butun <c>CaloraPerCoin</c> qismlari hisoblanadi.</summary>
    [Range(1, long.MaxValue)] public long Calora { get; set; }
}

public class ExchangeResultDto
{
    public long Coins { get; set; }
    public long CaloraSpent { get; set; }
    public WalletDto Wallet { get; set; } = null!;
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
