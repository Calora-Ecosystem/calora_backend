using BRB.Core.Common.Models.Base;
using Core.Enums;
using Microsoft.EntityFrameworkCore;

namespace Core.Entities.Billing;

[Index(nameof(Plan))]
public class PlanExtra : AuditableModelBase<long>
{
    public EnumSPlans Plan { get; set; }
    public long Fee { get; set; }
    public long OriginalFee { get; set; } = 0;
    public bool IsActive { get; set; }
    public int DurationInMonths { get; set; }
}