using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BRB.Core.Common.Models.Base;
using Core.Entities.Billing.Enum;
using Microsoft.EntityFrameworkCore;

namespace Core.Entities.Billing;

[Index(nameof(PlanId), nameof(FeatureKey), IsUnique = true)]
public class PlanFeature : AuditableModelBase<long>
{
    [ForeignKey(nameof(PlanExtra))]
    public long PlanId { get; set; }

    public EnumPlanFeature FeatureKey { get; set; }

    [MaxLength(255)]
    public string Value { get; set; } = null!;

    public PlanExtra PlanExtra { get; set; } = null!;
}