using BRB.Core.EF.Attributes;
using BRB.Core.EF.Extensions;
using Core.Brokers.DbContext;
using Core.Entities.Billing;
using Core.Services.Auth;
using Core.Services.Billing.Contracts;
using Core.Services.Billing.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Core.Services.Billing;

/// <summary>
/// Admin-facing management of user subscriptions from the dashboard. Lets a
/// SuperAdmin grant, edit or revoke a subscription directly, without going
/// through the payment flow handled by <see cref="OrderService"/>.
/// There is a unique index on <c>subscriptions.user_id</c>, so a user has at
/// most one subscription row; creating therefore upserts.
/// </summary>
[Injectable]
public class SubscriptionService(AppDbContext dbContext, AuthService authService)
{
    public async Task<GetSubscriptionDto> CreateOrUpdate(CreateOrUpdateSubscriptionDto dto)
    {
        await dbContext.Users.ExistsOrThrowsNotFoundException(dto.UserId);

        if (dto.EndsAt <= dto.StartsAt)
            throw new SubscriptionDateRangeInvalidException();

        var subscription = await dbContext.Subscriptions
            .FirstOrDefaultAsync(x => x.UserId == dto.UserId);

        if (subscription is null)
        {
            subscription = new Subscription { UserId = dto.UserId };
            dbContext.Subscriptions.Add(subscription);
        }

        subscription.SubscriptionPlan = dto.Plan;
        subscription.StartsAt = dto.StartsAt;
        subscription.EndsAt = dto.EndsAt;
        subscription.IsActive = dto.IsActive;

        await dbContext.SaveChangesAsync();

        // Force a token refresh so the plan change takes effect immediately,
        // mirroring the payment-accept flow.
        await authService.KillAllUserSessions(dto.UserId);

        return ToDto(subscription);
    }

    public async Task<GetSubscriptionDto?> GetByUserId(long userId)
    {
        var subscription = await dbContext.Subscriptions
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == userId);

        return subscription is null ? null : ToDto(subscription);
    }

    public async Task Delete(long userId)
    {
        var subscription = await dbContext.Subscriptions
            .FirstOrDefaultAsync(x => x.UserId == userId);

        if (subscription is null) return;

        dbContext.Subscriptions.Remove(subscription);
        await dbContext.SaveChangesAsync();

        await authService.KillAllUserSessions(userId);
    }

    private static GetSubscriptionDto ToDto(Subscription x) => new()
    {
        Id = x.Id,
        UserId = x.UserId,
        Plan = x.SubscriptionPlan,
        StartsAt = x.StartsAt,
        EndsAt = x.EndsAt,
        IsActive = x.IsActive
    };
}
