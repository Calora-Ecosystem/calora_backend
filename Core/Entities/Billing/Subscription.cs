using System.ComponentModel.DataAnnotations.Schema;
using BRB.Core.Common.Models.Base;
using Core.Entities.Auth;

namespace Core.Entities.Billing;

public class Subscription : AuditableModelBase<long>
{
    [ForeignKey(nameof(User))] public long UserId { get; set; }

    [ForeignKey(nameof(Plan))] public long PlanId { get; set; }

    public DateTime StartsAt { get; set; }
    public DateTime EndsAt { get; set; }
    public bool IsActive { get; set; }

    public User User { get; set; } = null!;
    public SubscriptionPlan Plan { get; set; } = null!;
}