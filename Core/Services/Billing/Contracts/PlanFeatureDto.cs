using Core.Entities.Billing.Enum;

namespace Core.Services.Billing.Contracts;

public class PlanFeatureDto
{
    public EnumPlanFeature FeatureKey { get; set; }

    public string Value { get; set; } = null!;
}
