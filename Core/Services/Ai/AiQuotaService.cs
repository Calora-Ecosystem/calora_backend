using BRB.Core.EF.Attributes;
using Core.Brokers.DbContext;
using Core.Enums;
using Core.Services.Ai.Contracts;
using Core.Services.Ai.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Core.Services.Ai;

/// <summary>
/// Premium bo'lmagan userlar uchun bepul AI limiti (rasm skan va ovoz bitta hovuzdan).
/// Limit butun dastur davomida amal qiladi (kunlik emas). Faqat muvaffaqiyatli
/// (bo'sh bo'lmagan) natija limitni kamaytiradi — mobile bilan bir xil qoida.
/// </summary>
[Injectable]
public class AiQuotaService(AppDbContext dbContext, IOptions<AiQuotaConfig> options)
{
    private int FreeLimit => options.Value.FreeLimit;

    public async Task<AiQuotaDto> Get(long userId, bool? isPremium = null)
    {
        isPremium ??= await IsPremium(userId);

        var quota = await dbContext.UserAiQuotas
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .Select(x => new { x.Used, x.BonusLimit })
            .FirstOrDefaultAsync();

        var limit = FreeLimit + (quota?.BonusLimit ?? 0);
        var used = quota?.Used ?? 0;

        return new AiQuotaDto
        {
            IsPremium = isPremium.Value,
            Unlimited = isPremium.Value,
            Limit = limit,
            Used = used,
            Remaining = Math.Max(0, limit - used)
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

    /// <summary>Muvaffaqiyatli AI natijasidan keyin bitta bepul so'rovni yechadi (atomik).</summary>
    public async Task Consume(long userId)
    {
        await EnsureRow(userId);

        var freeLimit = FreeLimit;
        await dbContext.UserAiQuotas
            .Where(x => x.UserId == userId && x.Used < freeLimit + x.BonusLimit)
            .ExecuteUpdateAsync(s => s
                .SetProperty(x => x.Used, x => x.Used + 1)
                .SetProperty(x => x.UpdatedAt, DateTime.Now));
    }

    /// <summary>Marketplace'dan olingan qo'shimcha bepul AI so'rovlar.</summary>
    public async Task AddBonus(long userId, int count)
    {
        await EnsureRow(userId);

        await dbContext.UserAiQuotas
            .Where(x => x.UserId == userId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(x => x.BonusLimit, x => x.BonusLimit + count)
                .SetProperty(x => x.UpdatedAt, DateTime.Now));
    }

    /// <summary>JWT <c>plan</c> claim bilan bir xil qoida: faol va Free emas.</summary>
    private Task<bool> IsPremium(long userId) =>
        dbContext.Subscriptions.AnyAsync(x =>
            x.UserId == userId && x.IsActive && x.SubscriptionPlan != EnumSPlans.Free);

    private async Task EnsureRow(long userId)
    {
        var now = DateTime.Now;
        await dbContext.Database.ExecuteSqlInterpolatedAsync($@"
insert into user_ai_quotas (user_id, used, bonus_limit, created_at, updated_at)
values ({userId}, 0, 0, {now}, {now})
on conflict (user_id) do nothing");
    }
}
