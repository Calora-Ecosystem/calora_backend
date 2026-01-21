using Core.Entities.Billing.Enum;
using Core.Enums;

namespace Core.Services.Billing.Contracts;

public class CreateSubscriptionOrderDto
{
    public EnumPaymentProviders Provider { get; set; }
    public EnumSPlans Plan { get; set; }
    public long PlanExtraId { get; set; }
    public long? CouponId { get; set; }
}