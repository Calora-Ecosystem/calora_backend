using System.ComponentModel.DataAnnotations.Schema;
using BRB.Core.Common.Models.Base;
using Core.Entities.Auth;
using Core.Entities.Billing.Enum;
using Core.Enums;
using Microsoft.EntityFrameworkCore;

namespace Core.Entities.Billing;

[Index(nameof(SubscriptionPlan))]
public class Subscription : AuditableModelBase<long>
{
    [ForeignKey(nameof(User))] public long UserId { get; set; }

    public EnumSPlans SubscriptionPlan { get; set; }

    public DateTime StartsAt { get; set; }
    public DateTime EndsAt { get; set; }
    public bool IsActive { get; set; }

    public EnumSubscriptionSource Source { get; set; } = EnumSubscriptionSource.Payment;

    /// <summary>
    /// Store (RevenueCat) boshqaradigan to'langan obuna ustiga qo'shilgan bonus kunlar
    /// (referral/coin). <c>EndsAt = store tugash sanasi + BonusDays</c>; RENEWAL/EXPIRATION
    /// hodisalari bonusni o'chirib yubormasligi uchun alohida saqlanadi.
    /// </summary>
    public int BonusDays { get; set; }

    public User User { get; set; } = null!;
}