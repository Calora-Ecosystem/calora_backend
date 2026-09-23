namespace Core.Services.Coins.Contracts;

/// <summary>
/// Coin iqtisodiyoti sozlamalari (appsettings: <c>"Coins"</c>). Hamma qiymatlarning
/// default'i bor, shuning uchun bo'lim yo'q bo'lsa ham ishlaydi.
/// </summary>
public class CoinConfig
{
    /// <summary>Nechta Calora (qadamdan yoqilgan kkal) 1 coinga teng.</summary>
    public int CaloraPerCoin { get; set; } = 1000;

    /// <summary>
    /// Calora shu sanadan boshlab hisoblanadi (feature ishga tushgan kun). Eski qadamlar
    /// bir zumda katta balans bermasligi uchun.
    /// </summary>
    public DateTime CaloraEarnStartDate { get; set; } = new(2026, 9, 23);

    /// <summary>
    /// Calora hisoblashda bir kunlik qadamning yuqori chegarasi (soxta qiymatlardan himoya).
    /// </summary>
    public int MaxDailySteps { get; set; } = 50_000;

    /// <summary>Har bir faol do'st uchun taklif qiluvchiga qo'shimcha coin (0 — o'chiq).</summary>
    public int ReferralReward { get; set; } = 0;

    /// <summary>Taklif kodi bilan kirgan yangi userga beriladigan coin (0 — o'chiq).</summary>
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
