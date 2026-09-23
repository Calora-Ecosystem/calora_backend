using Core.Entities.Billing.Enum;
using Core.Enums;

namespace Core.Services.Billing.Contracts;

/// <summary>
/// Profil → "Obuna" paneli uchun userning joriy obuna holati.
/// </summary>
public class GetMySubscriptionDto
{
    public EnumSPlans Plan { get; set; }

    /// <summary>Premium faolmi (JWT plan claim bilan bir xil qoida).</summary>
    public bool IsPremium { get; set; }

    public bool IsActive { get; set; }

    /// <summary>Obuna manbai: Payment, Admin, Coins yoki Referral.</summary>
    public EnumSubscriptionSource? Source { get; set; }
    public DateTime? StartsAt { get; set; }
    public DateTime? EndsAt { get; set; }
    public int DaysLeft { get; set; }

    /// <summary>Oxirgi tasdiqlangan to'lov provayderi (coin yoki admin orqali berilgan bo'lsa null).</summary>
    public EnumPaymentProviders? Provider { get; set; }

    /// <summary>Oxirgi sotib olingan paket davomiyligi (oy).</summary>
    public int? DurationInMonths { get; set; }

    /// <summary>
    /// Obuna holati: <c>Free</c> — premium yo'q, <c>Active</c> — faol,
    /// <c>Cancelled</c> — store'da bekor qilingan (premium <see cref="EndsAt"/> gacha qoladi).
    /// </summary>
    public EnumMySubscriptionStatus Status { get; set; }

    /// <summary>App Store / Google Play obunasi — tarif va bekor qilish store'ning o'zida.</summary>
    public bool ManagedByStore { get; set; }

    /// <summary>Obuna avtomatik yangilanadi (store obunasi va bekor qilinmagan).</summary>
    public bool AutoRenew { get; set; }

    /// <summary>Keyingi to'lov sanasi — faqat avtomatik yangilanadigan obunada.</summary>
    public DateTime? NextPaymentAt { get; set; }

    /// <summary>Bepul AI skan limiti holati (premiumda cheksiz).</summary>
    public Core.Services.Ai.Contracts.AiQuotaDto AiQuota { get; set; } = null!;
}

public enum EnumMySubscriptionStatus
{
    Free = 1,
    Active,
    Cancelled
}
