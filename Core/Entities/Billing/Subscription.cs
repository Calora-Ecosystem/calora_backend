using System.ComponentModel.DataAnnotations.Schema;
using BRB.Core.Common.Models.Base;
using Core.Entities.Auth;
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

    public User User { get; set; } = null!;
}