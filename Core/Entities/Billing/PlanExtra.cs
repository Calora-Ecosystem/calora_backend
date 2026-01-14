using BRB.Core.Common.Models.Base;
using Core.Enums;
using Microsoft.EntityFrameworkCore;

namespace Core.Entities.Billing;

[Index(nameof(Plan))]
public class PlanExtra : AuditableModelBase<long>
{
    public EnumSPlans Plan { get; set; }
    public long Fee { get; set; }
    public bool IsActive { get; set; }
    public TimeSpan Duration { get; set; }
}