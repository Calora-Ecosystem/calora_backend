using Swashbuckle.AspNetCore.Annotations;

namespace Core.Services.Billing.Contracts;

public class CheckCouponDto
{
    public long Id { get; set; }

    [SwaggerSchema("Coupon amount in tin.")]
    public long Amount { get; set; }

    public bool IsActive { get; set; }
    public DateTime? ExpireAt { get; set; }
}