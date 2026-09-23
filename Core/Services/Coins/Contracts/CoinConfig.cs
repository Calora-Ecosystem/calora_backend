namespace Core.Services.Coins.Contracts;

/// <summary>
/// Coin iqtisodiyoti sozlamalari (appsettings: <c>"Coins"</c>). Hamma qiymatlarning
/// default'i bor, shuning uchun bo'lim yo'q bo'lsa ham ishlaydi.
/// </summary>
public class CoinConfig
{
    /// <summary>Necha qadam uchun 1 coin beriladi.</summary>
    public int StepsPerCoin { get; set; } = 1000;

    /// <summary>
    /// Bir kunda qadamdan olinadigan maksimal coin (soxta qadam qiymatlaridan himoya).
    /// 7 000–22 000 qadam yuradigan user kuniga 7–22 coin yig'adi.
    /// </summary>
    public int MaxDailyCoins { get; set; } = 22;

    /// <summary>
    /// Coin shu sanadan boshlab hisoblanadi (feature ishga tushgan kun), eski qadamlar
    /// bir zumda katta balans bermasligi uchun.
    /// </summary>
    public DateTime CoinsEarnStartDate { get; set; } = new(2026, 9, 23);

    /// <summary>Har bir faol do'st uchun taklif qiluvchiga qo'shimcha coin (0 — o'chiq).</summary>
    public int ReferralReward { get; set; } = 0;

    /// <summary>Taklif kodi bilan kirgan userga beriladigan coin (0 — o'chiq).</summary>
    public int ReferredReward { get; set; } = 0;

    /// <summary>Nechta faol do'st uchun taklif qiluvchiga premium beriladi (har N ta uchun takrorlanadi).</summary>
    public int ReferralPremiumFriends { get; set; } = 5;

    /// <summary>Har <see cref="ReferralPremiumFriends"/> ta do'st uchun beriladigan premium kunlari.</summary>
    public int ReferralPremiumDays { get; set; } = 30;

    /// <summary>Referral orqali kelgan userga birinchi premium xaridida chegirma (%).</summary>
    public int ReferredDiscountPercent { get; set; } = 10;

    /// <summary>
    /// Taklif kodini ro'yxatdan o'tgandan keyin necha kun ichida tasdiqlash mumkin.
    /// 0 — cheklov yo'q: istalgan user (eski userlar ham) bir marta kod kirita oladi.
    /// </summary>
    public int ReferralApplyWindowDays { get; set; } = 0;

    /// <summary>Coupon turidagi marketplace mukofoti necha kun amal qiladi.</summary>
    public int CouponValidDays { get; set; } = 30;
}
