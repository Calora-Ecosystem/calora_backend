namespace Core.Entities.Coins.Enum;

/// <summary>
/// Marketplace mahsuloti sotib olinganda nima berilishi.
/// <see cref="Core.Entities.Coins.MarketItem.RewardValue"/> ma'nosi turga bog'liq.
/// </summary>
public enum EnumMarketRewardType
{
    /// <summary>Premium obuna; RewardValue = kunlar soni.</summary>
    PremiumDays = 1,

    /// <summary>Qo'shimcha bepul AI skanlar; RewardValue = skanlar soni.</summary>
    AiScans,

    /// <summary>Obuna uchun bir martalik chegirma kuponi; RewardValue = chegirma summasi (tiyin).</summary>
    Coupon,

    /// <summary>Oddiy vaucher (kod beriladi, operator qo'lda ishlaydi); RewardValue ishlatilmaydi.</summary>
    Voucher
}
