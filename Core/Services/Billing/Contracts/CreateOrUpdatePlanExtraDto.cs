using System.ComponentModel.DataAnnotations;
using Core.Enums;
using Swashbuckle.AspNetCore.Annotations;

namespace Core.Services.Billing.Contracts;

public class CreateOrUpdatePlanExtraDto
{
    [SwaggerSchema("Omit to create, pass to update")]
    public long? Id { get; set; }

    public EnumSPlans Plan { get; set; }

    [SwaggerSchema("Duration in months")]
    [Range(1, 120)]
    public int Duration { get; set; }

    [SwaggerSchema("Current price in so'm (UZS), stored as tiyn")]
    [Range(0, 1_000_000_000)]
    public double Fee { get; set; }

    [SwaggerSchema("Price before discount in so'm (UZS). 0 means no discount")]
    [Range(0, 1_000_000_000)]
    public double OriginalFee { get; set; }

    public bool IsActive { get; set; }

    [SwaggerSchema("Marks this package as the \"best offer\". Only one per plan")]
    public bool IsPopular { get; set; }
}
