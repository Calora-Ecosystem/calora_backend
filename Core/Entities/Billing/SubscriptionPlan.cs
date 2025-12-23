using BRB.Core.Common.Models;
using BRB.Core.Common.Models.Base;

namespace Core.Entities.Billing;

public class SubscriptionPlan : AuditableModelBase<long>
{
    public MultiLanguageField Name { get; set; } = null!;
    public decimal Fee { get; set; }
    public bool IsActive { get; set; }
}