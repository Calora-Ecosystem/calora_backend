using System.ComponentModel.DataAnnotations.Schema;
using BRB.Core.Common.Models.Base;
using Core.Entities.Auth;
using Core.Entities.Billing.Enum;
using Microsoft.EntityFrameworkCore;

namespace Core.Entities.Billing;

[Index(nameof(Status))]
[Index(nameof(Type))]
public class Order : AuditableModelBase<long>
{
    [ForeignKey(nameof(User))] public long UserId { get; set; }
    public required long Amount { get; set; }
    public EnumPaymentProviders Provider { get; set; }
    public EnumOrderType Type { get; set; }
    public EnumOrderStatus Status { get; set; }
    public User User { get; set; } = null!;
    [ForeignKey(nameof(Coupon))] public long? CouponId { get; set; }
    public Coupon? Coupon { get; set; } = null!;

    /// <summary>Referral orqali kelgan userga berilgan chegirma summasi (tiyin).</summary>
    public long ReferralDiscount { get; set; }
}