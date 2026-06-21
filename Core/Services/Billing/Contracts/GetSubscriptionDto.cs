using Core.Enums;

namespace Core.Services.Billing.Contracts;

public class GetSubscriptionDto
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public EnumSPlans Plan { get; set; }
    public DateTime StartsAt { get; set; }
    public DateTime EndsAt { get; set; }
    public bool IsActive { get; set; }
}
