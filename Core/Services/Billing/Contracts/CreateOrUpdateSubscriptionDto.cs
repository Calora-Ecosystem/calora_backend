using Core.Enums;

namespace Core.Services.Billing.Contracts;

/// <summary>
/// Admin payload to grant or edit a user's subscription directly from the
/// dashboard (bypasses the payment flow). One subscription per user, so this
/// upserts the existing row when one already exists for <see cref="UserId"/>.
/// </summary>
public class CreateOrUpdateSubscriptionDto
{
    public long UserId { get; set; }
    public EnumSPlans Plan { get; set; }
    public DateTime StartsAt { get; set; }
    public DateTime EndsAt { get; set; }
    public bool IsActive { get; set; } = true;
}
