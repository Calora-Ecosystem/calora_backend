using Core.Entities.Billing.Enum;
using Core.Enums;

namespace Core.Services.Dashboard.Contracts;

public record GetSubscriptionOrdersDto
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public string UserName { get; set; } = null!;
    public EnumSPlans Plan { get; set; }
    public long PlanExtraId { get; set; }
    public int PlanExtraDurationInMonths { get; set; }
    public EnumOrderStatus OrderStatus { get; set; }
    public double Amount { get; set; }
    public DateTime CreatedAt { get; set; }
    public EnumPaymentProviders PaymentProvider { get; set; }
    public CouponDto? Coupon { get; set; }
}

public record CouponDto
{
    public long Id { get; set; }
    public string Code { get; set; } = null!;
}