using System.ComponentModel.DataAnnotations;
using Swashbuckle.AspNetCore.Annotations;

namespace Core.Services.Billing.Contracts;

public class CreateOrUpdateCouponDto
{
    public long? Id { get; set; }
    public string Code { get; set; } = null!;
    public bool OneTime { get; set; }
    public bool IsActive { get; set; }

    [SwaggerSchema("Coupon amount in tiyn")]
    [Range(1, long.MaxValue)]
    public long Amount { get; set; }

    public List<long>? AllowedUserIds { get; set; }
    public DateTime? ExpireAt { get; set; }
}