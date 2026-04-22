using Core.Enums;

namespace Core.Services.User.Contracts;

public record SubscriptionDto
{
    public long Id { get; set; }
    public DateTime StartsAt { get; set; }
    public DateTime EndsAt { get; set; }
    public EnumSPlans Plan { get; set; }
    public bool IsActive { get; set; }
}