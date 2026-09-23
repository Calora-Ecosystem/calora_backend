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

    /// <summary>Store (IAP) obunasi avtomatik yangilanadi.</summary>
    public bool AutoRenew { get; set; }

    /// <summary>Keyingi to'lov sanasi — faqat avtomatik yangilanadigan obunada.</summary>
    public DateTime? NextPaymentAt { get; set; }

    /// <summary>Bepul AI skan limiti holati (premiumda cheksiz).</summary>
    public Core.Services.Ai.Contracts.AiQuotaDto AiQuota { get; set; } = null!;
}
