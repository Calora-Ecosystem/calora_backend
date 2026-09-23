using BRB.Core.EF.Attributes;
using Core.Brokers.DbContext;
using Core.Services.Coins.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Core.Services.Coins;

/// <summary>
/// Referral orqali kelgan user birinchi premium xaridida <see cref="CoinConfig.ReferredDiscountPercent"/>%
/// chegirma oladi. Chegirma to'lov tasdiqlanganda "ishlatilgan" deb belgilanadi
/// (bekor qilingan buyurtma chegirmani yo'qotmaydi).
/// </summary>
[Injectable]
public class ReferralDiscountService(AppDbContext dbContext, IOptions<CoinConfig> options)
{
    /// <summary>User hozir foydalana oladigan chegirma foizi (0 — yo'q).</summary>
    public async Task<int> GetAvailablePercent(long userId)
    {
        var percent = options.Value.ReferredDiscountPercent;
        if (percent <= 0) return 0;

        var available = await dbContext.Referrals
            .AnyAsync(x => x.ReferredUserId == userId && x.DiscountUsedAt == null);

        return available ? percent : 0;
    }

    /// <summary>Summa (tiyin) uchun chegirma miqdori.</summary>
    public static long Calculate(long amount, int percent) => amount * percent / 100;

    /// <summary>To'lov tasdiqlanganda chaqiriladi.</summary>
    public async Task MarkUsed(long userId, long orderId)
    {
        await dbContext.Referrals
            .Where(x => x.ReferredUserId == userId && x.DiscountUsedAt == null)
            .ExecuteUpdateAsync(s => s
                .SetProperty(x => x.DiscountUsedAt, DateTime.Now)
                .SetProperty(x => x.DiscountOrderId, orderId));
    }
}
