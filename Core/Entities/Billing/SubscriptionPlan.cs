using BRB.Core.Common.Models;

namespace Core.Entities.Billing;

public class SubscriptionPlan
{
    public MultiLanguageField Name { get; set; } = null!;
    public decimal Fee { get; set; }
    public bool IsActive { get; set; }
}