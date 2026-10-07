using System.ComponentModel.DataAnnotations.Schema;
using BRB.Core.Common.Models.Base;
using Core.Entities.Auth;
using Core.Entities.Billing.Enum;
using Microsoft.EntityFrameworkCore;

namespace Core.Entities.Billing;

/// <summary>
/// Foydalanuvchining ma'lum bir funksiya (feature) bo'yicha ishlatgan va bonus limitlari.
/// </summary>
[Index(nameof(UserId), nameof(FeatureKey), IsUnique = true)]
public class UserFeatureUsage : AuditableModelBase<long>
{
    [ForeignKey(nameof(User))]
    public long UserId { get; set; }

    public EnumPlanFeature FeatureKey { get; set; }

    public int Used { get; set; }

    public int BonusLimit { get; set; }

    public User User { get; set; } = null!;
}
