using System.ComponentModel.DataAnnotations.Schema;
using BRB.Core.Common.Models.Base;
using Microsoft.EntityFrameworkCore;

namespace Core.Entities.Billing;

[Index(nameof(PlanId), nameof(FeatureKey), IsUnique = true)]
public class PlanFeature : AuditableModelBase<long>
{
    [ForeignKey(nameof(PlanExtra))]
    public long PlanId { get; set; }
    public string FeatureKey { get; set; } = null!;
    public string Limit { get; set; } = null!;
    public PlanExtra PlanExtra { get; set; } = null!;
}