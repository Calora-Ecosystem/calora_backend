using BRB.Core.EF.Attributes;
using BRB.Core.EF.Extensions;
using Core.Brokers.DbContext;
using Core.Entities.Billing;
using Core.Entities.Billing.Enum;
using Core.Enums;
using Core.Services.Ai;
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
public class SubscriptionService(AppDbContext dbContext, AuthService authService, AiQuotaService aiQuotaService)
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
        subscription.Source = EnumSubscriptionSource.Admin;

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

    public async Task<GetMySubscriptionDto> GetMy(long userId)
    {
        var now = DateTime.Now;

        var subscription = await dbContext.Subscriptions
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == userId);

        var lastOrder = await dbContext.SubscriptionOrders
            .AsNoTracking()
            .Where(x => x.Order.UserId == userId
                        && x.Order.Type == EnumOrderType.Subscription
                        && x.Order.Status == EnumOrderStatus.Confirmed)
            .OrderByDescending(x => x.Order.UpdatedAt)
            .Select(x => new { x.Order.Provider, x.PlanExtra.DurationInMonths })
            .FirstOrDefaultAsync();

        // JWT plan claim bilan bir xil qoida (AuthService.MakeJwtFromUser).
        var isPremium = IsPremium(subscription);

        var managedByStore = isPremium && subscription!.Source == EnumSubscriptionSource.Payment &&
                             lastOrder?.Provider == EnumPaymentProviders.Iap;
        var cancelled = managedByStore && subscription!.CancelledAt != null;
        var autoRenew = managedByStore && !cancelled;

        return new GetMySubscriptionDto
        {
            Plan = isPremium ? subscription!.SubscriptionPlan : EnumSPlans.Free,
            IsPremium = isPremium,
            IsActive = subscription?.IsActive ?? false,
            Source = subscription?.Source,
            StartsAt = subscription?.StartsAt,
            EndsAt = subscription?.EndsAt,
            Status = !isPremium
                ? EnumMySubscriptionStatus.Free
                : cancelled ? EnumMySubscriptionStatus.Cancelled : EnumMySubscriptionStatus.Active,
            ManagedByStore = managedByStore,
            DaysLeft = isPremium ? Math.Max(0, (int)Math.Ceiling((subscription!.EndsAt - now).TotalDays)) : 0,
            Provider = lastOrder?.Provider,
            DurationInMonths = lastOrder?.DurationInMonths,
            AutoRenew = autoRenew,
            NextPaymentAt = autoRenew ? subscription!.EndsAt : null,
            AiQuota = await aiQuotaService.Get(userId, isPremium)
        };
    }

    /// <summary>
    /// Premium'ni <paramref name="days"/> kunga beradi yoki uzaytiradi (coin xaridi, referral mukofoti).
    /// Faol obuna bo'lsa max(EndsAt, hozir) ga qo'shiladi va manbasi o'zgarmaydi (to'langan obuna
    /// "Coins"/"Referral" bo'lib qolib, job tomonidan o'chirilib ketmasligi uchun);
    /// aks holda hozirdan boshlanadi va manba <paramref name="source"/> bo'ladi.
    /// Chaqiruvchi tranzaksiya ichida chaqirishi mumkin.
    /// </summary>
    public async Task GrantPremiumDays(long userId, int days, EnumSubscriptionSource source)
    {
        var now = DateTime.Now;

        var subscription = await dbContext.Subscriptions.FirstOrDefaultAsync(x => x.UserId == userId);

        if (subscription is null)
        {
            subscription = new Subscription { UserId = userId };
            dbContext.Subscriptions.Add(subscription);
        }

        if (subscription.Id > 0 && IsPremium(subscription))
        {
            // Store (IAP) obunasida EndsAt RevenueCat bilan sinxronlanadi — bonusni alohida saqlaymiz.
            if (subscription.Source == EnumSubscriptionSource.Payment && await IsStoreManaged(userId))
                subscription.BonusDays += days;

            subscription.EndsAt = (subscription.EndsAt > now ? subscription.EndsAt : now).AddDays(days);
        }
        else
        {
            subscription.SubscriptionPlan = EnumSPlans.Premium;
            subscription.StartsAt = now;
            subscription.EndsAt = now.AddDays(days);
            subscription.IsActive = true;
            subscription.Source = source;
        }

        await dbContext.SaveChangesAsync();

        // Plan claim JWT ichida — token yangilanishi uchun sessiyalarni tozalaymiz.
        await authService.KillAllUserSessions(userId);
    }

    /// <summary>
    /// Recurring job: muddati o'tgan Coins/Referral obunalarini o'chiradi. To'langan (Payment)
    /// obunalar bu yerda o'chirilmaydi — ularni to'lov provayderi boshqaradi.
    /// </summary>
    public async Task DeactivateExpiredGrants()
    {
        var now = DateTime.Now;

        var expired = dbContext.Subscriptions.Where(x =>
            x.IsActive && x.EndsAt <= now &&
            (x.Source == EnumSubscriptionSource.Coins || x.Source == EnumSubscriptionSource.Referral));

        var userIds = await expired.Select(x => x.UserId).ToListAsync();
        if (userIds.Count == 0) return;

        await dbContext.Subscriptions
            .Where(x => userIds.Contains(x.UserId) && x.IsActive && x.EndsAt <= now)
            .ExecuteUpdateAsync(s => s
                .SetProperty(x => x.IsActive, false)
                .SetProperty(x => x.UpdatedAt, now));

        foreach (var userId in userIds)
            await authService.KillAllUserSessions(userId);
    }

    /// <summary>Oxirgi tasdiqlangan obuna to'lovi IAP (RevenueCat) orqali bo'lganmi.</summary>
    private async Task<bool> IsStoreManaged(long userId) =>
        await dbContext.SubscriptionOrders
            .Where(x => x.Order.UserId == userId && x.Order.Status == EnumOrderStatus.Confirmed)
            .OrderByDescending(x => x.Order.UpdatedAt)
            .Select(x => (EnumPaymentProviders?)x.Order.Provider)
            .FirstOrDefaultAsync() == EnumPaymentProviders.Iap;

    /// <summary>Premium = faol va Free emas. JWT <c>plan</c> claim ham shu qoidada.</summary>
    public static bool IsPremium(Subscription? subscription) =>
        subscription is { IsActive: true } && subscription.SubscriptionPlan != EnumSPlans.Free;

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
