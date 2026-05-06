using Core.Enums;
using Swashbuckle.AspNetCore.Annotations;

namespace Core.Services.Billing.Contracts;

public record GetPlanExtras
{
    public long Id { get; set; }
    [SwaggerSchema("Duration in months")] public double Duration { get; set; }
    public bool IsActive { get; set; }
    public EnumSPlans Plan { get; set; }
    public DateTime CreatedAt { get; set; }
    public double Fee { get; set; }
    public double OriginalFee { get; set; }
    public bool IsPopular { get; set; }
}