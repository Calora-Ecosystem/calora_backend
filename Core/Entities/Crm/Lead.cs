using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BRB.Core.Common.Models.Base;
using Core.Entities.Auth;
using Core.Entities.Billing.Enum;
using Core.Entities.Crm.Enum;
using Microsoft.EntityFrameworkCore;

namespace Core.Entities.Crm;

[Index(nameof(Priority))]
[Index(nameof(Status))]
[Index(nameof(Score))]
[Index(nameof(OperatorId))]
[Index(nameof(NextFollowUpAt))]
[Index(nameof(SubscriptionOpenedCount))]
public class Lead : ModelBase<long>
{
    [ForeignKey(nameof(User))]
    public long UserId { get; set; }

    [ForeignKey(nameof(Operator))]
    public long? OperatorId { get; set; }

    public bool IsRegistered { get; set; }
    public uint SubscriptionOpenedCount { get; set; }
    public bool Purchased { get; set; }

    // Behaviour counters used by the scoring engine.
    public uint WorkoutStartedCount { get; set; }
    public uint WaterTrackedCount { get; set; }
    public uint FoodTrackedCount { get; set; }
    public uint AppOpenCount { get; set; }

    // Legacy priority (kept for backward compatibility). Status + Score are primary.
    public EnumLeadPriority Priority { get; set; }

    public EnumLeadStatus Status { get; set; } = EnumLeadStatus.New;
    public int Score { get; set; }
    public EnumLeadTemperature Temperature { get; set; } = EnumLeadTemperature.Cold;

    public DateTime LastActivity { get; set; }
    public DateTime? LastContactedAt { get; set; }
    public DateTime? NextFollowUpAt { get; set; }

    public EnumPaymentProviders? PaymentProvider { get; set; }
    public long? WonAmount { get; set; }
    public DateTime? WonAt { get; set; }

    // Premium acquisition: when the winning order used a coupon these are set
    // (promo-code path); otherwise the lead got premium via a platform purchase.
    public long? CouponId { get; set; }
    [MaxLength(50)] public string? PromoCode { get; set; }
    public DateTime? LostAt { get; set; }
    [MaxLength(300)] public string? LostReason { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public User User { get; set; } = null!;
    public User? Operator { get; set; }
}
