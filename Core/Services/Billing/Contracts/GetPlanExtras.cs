using Core.Enums;
using Swashbuckle.AspNetCore.Annotations;

namespace Core.Services.Billing.Contracts;

public record GetPlanExtras
{
    public long Id { get; set; }
    [SwaggerSchema("Duration in days")]
    public double Duration { get; set; }
    public bool IsActive { get; set; }
    public EnumSPlans Plan { get; set; }
    public DateTime CreatedAt { get; set; }
}