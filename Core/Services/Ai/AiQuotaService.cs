using BRB.Core.EF.Attributes;
using Core.Brokers.DbContext;
using Core.Entities.Billing;
using Core.Entities.Billing.Enum;
using Core.Enums;
using Core.Services.Ai.Contracts;
using Core.Services.Ai.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Core.Services.Ai;

/// <summary>
/// AI (rasm skan va ovoz) foydalanish limiti servisi.
/// Limit yagona haqiqat manbasi (single source of truth) bo'lgan <see cref="PlanFeature"/> orqali aniqlanadi.
/// </summary>
[Injectable]
public class AiQuotaService(AppDbContext dbContext)
{
    public const EnumPlanFeature Feature = EnumPlanFeature.AiScans;

    public async Task<AiQuotaDto> Get(long userId, bool? isPremium = null)
    {
        var userPlan = await dbContext.Subscriptions
            .AsNoTracking()
            .Where(x => x.UserId == userId && x.IsActive)
            .Select(x => x.SubscriptionPlan)
            .FirstOrDefaultAsync();

        var isPrem = isPremium ?? (userPlan != default && userPlan != EnumSPlans.Free);
        var effectivePlan = isPrem ? (userPlan != default ? userPlan : EnumSPlans.Premium) : EnumSPlans.Free;

        var (isUnlimited, baseLimit) = await ResolvePlanFeatureLimit(effectivePlan);

        if (isUnlimited)
        {
            return new AiQuotaDto
            {
                IsPremium = true,
                Unlimited = true,
                Limit = 0,
                Used = 0,
                Remaining = 999999
            };
        }

        var usage = await dbContext.UserFeatureUsages
            .AsNoTracking()
            .Where(x => x.UserId == userId && x.FeatureKey == Feature)
            .Select(x => new { x.Used, x.BonusLimit })
            .FirstOrDefaultAsync();

        var totalLimit = baseLimit + (usage?.BonusLimit ?? 0);
        var used = usage?.Used ?? 0;

        return new AiQuotaDto
        {
            IsPremium = false,
            Unlimited = false,
            Limit = totalLimit,
            Used = used,
            Remaining = Math.Max(0, totalLimit - used)
        };
    }

    /// <summary>
    /// AI chaqiruvidan oldin. Premium bo'lsa true (hisoblanmaydi), bepul limit tugagan bo'lsa
    /// <see cref="AiFreeLimitExceededException"/>. Qaytgan qiymat — so'rov limitdan yechiladimi.
    /// </summary>
    public async Task<bool> EnsureCanUse(long userId)
    {
        if (await IsPremium(userId))
            return false;

        var quota = await Get(userId, isPremium: false);

        if (quota.Remaining <= 0)
            throw new AiFreeLimitExceededException();

        return true;
    }

    /// <summary>Muvaffaqiyatli AI natijasidan keyin bitta bepul so'rovni yechadi.</summary>
    public async Task Consume(long userId)
    {
        var feature = await dbContext.UserFeatureUsages
                          .FirstOrDefaultAsync(x => x.UserId == userId && x.FeatureKey == Feature)
                      ?? new UserFeatureUsage
                      {
                          FeatureKey = Feature,
                          UserId = userId
                      };

        feature.Used += 1;
        dbContext.UserFeatureUsages.Update(feature);
        await dbContext.SaveChangesAsync();
    }

    /// <summary>Marketplace'dan olingan qo'shimcha bepul AI so'rovlar.</summary>
    public async Task AddBonus(long userId, int count)
    {
        var feature = await dbContext.UserFeatureUsages
                          .FirstOrDefaultAsync(x => x.UserId == userId && x.FeatureKey == Feature)
                      ?? new UserFeatureUsage
                      {
                          FeatureKey = Feature,
                          UserId = userId
                      };

        feature.BonusLimit += count;
        dbContext.UserFeatureUsages.Update(feature);
        await dbContext.SaveChangesAsync();
    }

    /// <summary>JWT <c>plan</c> claim bilan bir xil qoida: faol va Free emas.</summary>
    private Task<bool> IsPremium(long userId) =>
        dbContext.Subscriptions.AnyAsync(x =>
            x.UserId == userId && x.IsActive && x.SubscriptionPlan != EnumSPlans.Free);

    private async Task<(bool isUnlimited, int baseLimit)> ResolvePlanFeatureLimit(EnumSPlans plan)
    {
        var featureValue = await dbContext.PlanFeatures
            .AsNoTracking()
            .Where(f => f.PlanExtra.Plan == plan && f.FeatureKey == Feature && f.PlanExtra.IsActive)
            .Select(f => f.Value)
            .FirstOrDefaultAsync();

        if (featureValue != null)
        {
            if (string.Equals(featureValue.Trim(), "unlimited", StringComparison.OrdinalIgnoreCase))
                return (true, 0);

            if (int.TryParse(featureValue, out var configuredLimit))
                return (false, configuredLimit);
        }

        return (plan > EnumSPlans.Free /*in this case, if plan != free, unlimited*/, 0);
    }
}