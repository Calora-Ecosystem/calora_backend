using System.ComponentModel.DataAnnotations.Schema;
using BRB.Core.Common.Models.Base;
using Microsoft.EntityFrameworkCore;

namespace Core.Entities.Billing;

[Index(nameof(PlanId), nameof(FeatureKey), IsUnique = true)]
public class PlanFeatures : AuditableModelBase<long>
{
    [ForeignKey(nameof(Plan))]
    public long PlanId { get; set; }
    public string FeatureKey { get; set; } = null!;
    public string Limit { get; set; } = null!;
    public SubscriptionPlan Plan { get; set; } = null!;
}