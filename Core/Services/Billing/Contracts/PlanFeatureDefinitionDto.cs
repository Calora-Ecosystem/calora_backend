using Core.Entities.Billing.Enum;

namespace Core.Services.Billing.Contracts;

/// <summary>
/// Tizimdagi mavjud feature key va uning ta'rifi.
/// </summary>
public class PlanFeatureDefinitionDto
{
    public EnumPlanFeature FeatureKey { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }
}
