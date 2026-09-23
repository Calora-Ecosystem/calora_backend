using BRB.Core.Common.Models;
using BRB.Core.EF.Attributes;
using BRB.Core.EF.Extensions;
using Core.Brokers.DbContext;
using Core.Entities.Billing;
using Core.Entities.Billing.Enum;
using Core.Entities.Coins;
using Core.Entities.Coins.Enum;
using Core.Enums;
using Core.Helpers;
using Core.Services.Ai;
using Core.Services.Billing;
using Core.Services.Coins.Contracts;
using Core.Services.Coins.Exceptions;
using Core.Services.User.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ResultWrapper.Library;

namespace Core.Services.Coins;

/// <summary>
/// Coin hamyoni: qadamlardan avtomatik yig'iladigan coinlar (har <see cref="CoinConfig.StepsPerCoin"/>
/// qadam = 1 coin, kuniga ko'pi bilan <see cref="CoinConfig.MaxDailyCoins"/>), marketplace xaridlari,
/// tarix va coin reytingi. Balans o'zgarishlari atomik (<c>ExecuteUpdate</c> / qatorni qulflash) —
/// parallel so'rovlarda manfiy balans yoki ikki marta berilgan coin bo'lmaydi.
/// </summary>
[Injectable]
public class CoinService(
    AppDbContext dbContext,
    SubscriptionService subscriptionService,
    AiQuotaService aiQuotaService,
    IOptions<CoinConfig> options)
{
    private const string DailyStepsTitle = "coin_tx_daily_steps";
    private CoinConfig Config => options.Value;

    #region Wallet

    public async Task<WalletDto> GetWallet(long userId)
    {
        await SyncStepCoins(userId);

        var wallet = await dbContext.CoinWallets
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .Select(x => new { x.Balance, x.TotalEarned, x.TotalSpent })
            .FirstOrDefaultAsync();

        var todayRef = DayRef(DateTime.Now.Date);
        var todayCoins = await dbContext.CoinTransactions
            .Where(x => x.UserId == userId && x.Type == EnumCoinTxType.Steps && x.RefId == todayRef)
            .Select(x => x.Amount)
            .FirstOrDefaultAsync();

        return new WalletDto
        {
            Balance = wallet?.Balance ?? 0,
            TotalEarned = wallet?.TotalEarned ?? 0,
            TotalSpent = wallet?.TotalSpent ?? 0,
            TodayCoins = todayCoins,
            StepsPerCoin = Config.StepsPerCoin,
            MaxDailyCoins = Config.MaxDailyCoins
        };
    }

    public async Task<Wrapper> GetTransactions(long userId, DataQueryRequest q, EnumCoinTxType? type = null)
    {
        return await dbContext.CoinTransactions
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .Where(x => type == null || x.Type == type)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new CoinTransactionDto
            {
                Id = x.Id,
                Title = x.Title,
                Amount = x.Amount,
                Type = x.Type,
                CreatedAt = x.CreatedAt
            })
            .GetByDataQueryAsync(q);
    }

    /// <summary>
    /// Qadamlar uchun coinlarni hamyonga yozadi: har kun uchun
    /// <c>min(qadam / StepsPerCoin, MaxDailyCoins)</c> coin, kuniga bitta tarix yozuvi.
    /// Qadam faqat oshib boradi (<c>users/dailies</c>), shuning uchun kun yozuvi ham faqat oshadi —
    /// qayta chaqirish yoki kunni reset qilish coinni ikki marta bermaydi.
    /// Hamyon qatori <c>FOR UPDATE</c> bilan qulflanadi — parallel sinxronlar farqni ikki marta qo'shmaydi.
    /// </summary>
    public async Task SyncStepCoins(long userId)
    {
        var createdAt = await dbContext.Users
            .Where(x => x.Id == userId)
            .Select(x => (DateTime?)x.CreatedAt)
            .FirstOrDefaultAsync();

        if (createdAt is null || Config.StepsPerCoin <= 0)
            return;

        var start = createdAt.Value.Date > Config.CoinsEarnStartDate.Date
            ? createdAt.Value.Date
            : Config.CoinsEarnStartDate.Date;

        var days = await dbContext.UserDailies
            .AsNoTracking()
            .Where(x => x.UserId == userId && x.Metric == EnumMetrics.Step && x.Date >= start)
            .Select(x => new { x.Date, x.Value })
            .ToListAsync();

        var expected = days
            .GroupBy(x => x.Date.Date)
            .Select(g => new
            {
                Day = g.Key,
                Coins = Math.Min((long)(g.Max(x => x.Value) / Config.StepsPerCoin), Config.MaxDailyCoins)
            })
            .Where(x => x.Coins > 0)
            .ToList();

        if (expected.Count == 0)
            return;

        await EnsureWallet(userId);

        await dbContext.Transactional(async () =>
        {
            await dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"select 1 from coin_wallets where user_id = {userId} for update");

            var credited = await dbContext.CoinTransactions
                .Where(x => x.UserId == userId && x.Type == EnumCoinTxType.Steps)
                .ToDictionaryAsync(x => x.RefId ?? 0);

            var now = DateTime.Now;
            long delta = 0;

            foreach (var day in expected)
            {
                var dayRef = DayRef(day.Day);

                if (credited.TryGetValue(dayRef, out var tx))
                {
                    if (tx.Amount >= day.Coins) continue;

                    delta += day.Coins - tx.Amount;
                    tx.Amount = day.Coins;
                }
                else
                {
                    delta += day.Coins;
                    dbContext.CoinTransactions.Add(new CoinTransaction
                    {
                        UserId = userId,
                        Amount = day.Coins,
                        Type = EnumCoinTxType.Steps,
                        Title = DailyStepsTitle,
                        RefId = dayRef
                    });
                }
            }

            if (delta == 0)
                return;

            await dbContext.CoinWallets
                .Where(x => x.UserId == userId)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(x => x.Balance, x => x.Balance + delta)
                    .SetProperty(x => x.TotalEarned, x => x.TotalEarned + delta)
                    .SetProperty(x => x.UpdatedAt, now));

            await dbContext.SaveChangesAsync();
        });
    }

    /// <summary>
    /// Hamyonga coin qo'shadi (referral bonus va h.k.). Chaqiruvchi tranzaksiya ichida bo'lishi kerak;
    /// tarix yozuvi <c>SaveChanges</c> bilan saqlanadi.
    /// </summary>
    public async Task Credit(long userId, long amount, EnumCoinTxType type, string title, long? refId = null)
    {
        if (amount <= 0) return;

        await EnsureWallet(userId);

        var now = DateTime.Now;
        await dbContext.CoinWallets
            .Where(x => x.UserId == userId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(x => x.Balance, x => x.Balance + amount)
                .SetProperty(x => x.TotalEarned, x => x.TotalEarned + amount)
                .SetProperty(x => x.UpdatedAt, now));

        dbContext.CoinTransactions.Add(new CoinTransaction
        {
            UserId = userId,
            Amount = amount,
            Type = type,
            Title = title,
            RefId = refId
        });

        await dbContext.SaveChangesAsync();
    }

    public async Task<long> GetBalance(long userId) =>
        await dbContext.CoinWallets
            .Where(x => x.UserId == userId)
            .Select(x => x.Balance)
            .FirstOrDefaultAsync();

    /// <summary>Qadam kunining kaliti (yyyyMMdd) — <see cref="CoinTransaction.RefId"/>.</summary>
    private static long DayRef(DateTime day) => day.Year * 10000L + day.Month * 100 + day.Day;

    private async Task EnsureWallet(long userId)
    {
        var now = DateTime.Now;
        await dbContext.Database.ExecuteSqlInterpolatedAsync($@"
insert into coin_wallets (user_id, balance, total_earned, total_spent, created_at, updated_at)
values ({userId}, 0, 0, 0, {now}, {now})
on conflict (user_id) do nothing");
    }

    #endregion

    #region Marketplace

    public async Task<Wrapper> GetMarketItems(DataQueryRequest q, EnumMarketCategory? category = null,
        bool onlyActive = true)
    {
        return await dbContext.MarketItems
            .AsNoTracking()
            .Where(x => !onlyActive || x.IsActive)
            .Where(x => category == null || x.Category == category)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.PriceCoins)
            .Select(x => new MarketItemDto
            {
                Id = x.Id,
                Title = x.Title,
                Subtitle = x.Subtitle,
                PriceCoins = x.PriceCoins,
                Category = x.Category,
                RewardType = x.RewardType,
                RewardValue = x.RewardValue,
                IsPopular = x.IsPopular,
                IsActive = x.IsActive,
                SortOrder = x.SortOrder
            })
            .GetByDataQueryAsync(q);
    }

    public async Task<PurchaseResultDto> Purchase(long userId, long marketItemId)
    {
        var item = await dbContext.MarketItems
                       .AsNoTracking()
                       .FirstOrDefaultAsync(x => x.Id == marketItemId && x.IsActive)
                   ?? throw new MarketItemNotFoundException();

        await EnsureWallet(userId);

        MarketPurchase purchase = null!;

        await dbContext.Transactional(async () =>
        {
            var now = DateTime.Now;
            var price = item.PriceCoins;

            // Balans DB ichida tekshiriladi — parallel xaridlar balansni manfiy qila olmaydi.
            var affected = await dbContext.CoinWallets
                .Where(x => x.UserId == userId && x.Balance >= price)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(x => x.Balance, x => x.Balance - price)
                    .SetProperty(x => x.TotalSpent, x => x.TotalSpent + price)
                    .SetProperty(x => x.UpdatedAt, now));

            if (affected == 0)
                throw new InsufficientCoinsException();

            purchase = dbContext.MarketPurchases.Add(new MarketPurchase
            {
                UserId = userId,
                MarketItemId = item.Id,
                Title = item.Title,
                PriceCoins = price,
                RewardType = item.RewardType,
                RewardValue = item.RewardValue
            }).Entity;

            await dbContext.SaveChangesAsync();

            switch (item.RewardType)
            {
                case EnumMarketRewardType.PremiumDays:
                    await subscriptionService.GrantPremiumDays(userId, (int)item.RewardValue, EnumSubscriptionSource.Coins);
                    break;
                case EnumMarketRewardType.AiScans:
                    await aiQuotaService.AddBonus(userId, (int)item.RewardValue);
                    break;
                case EnumMarketRewardType.Coupon:
                    purchase.Code = await GenerateUniqueCouponCode();
                    dbContext.Coupons.Add(new Coupon
                    {
                        Code = purchase.Code,
                        AllowedUserIds = [userId],
                        OneTime = true,
                        IsActive = true,
                        Amount = item.RewardValue,
                        ExpireAt = now.AddDays(Config.CouponValidDays)
                    });
                    break;
                case EnumMarketRewardType.Voucher:
                    purchase.Code = CodeGenerator.Generate("V-", 8);
                    break;
            }

            dbContext.CoinTransactions.Add(new CoinTransaction
            {
                UserId = userId,
                Amount = -price,
                Type = EnumCoinTxType.Purchase,
                Title = item.Title,
                RefId = purchase.Id
            });

            await dbContext.SaveChangesAsync();
        });

        return new PurchaseResultDto
        {
            Purchase = ToDto(purchase),
            Wallet = await GetWallet(userId),
            RequiresTokenRefresh = item.RewardType == EnumMarketRewardType.PremiumDays
        };
    }

    public async Task<Wrapper> GetPurchases(long userId, DataQueryRequest q)
    {
        return await dbContext.MarketPurchases
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new MarketPurchaseDto
            {
                Id = x.Id,
                MarketItemId = x.MarketItemId,
                Title = x.Title,
                PriceCoins = x.PriceCoins,
                RewardType = x.RewardType,
                RewardValue = x.RewardValue,
                Code = x.Code,
                CreatedAt = x.CreatedAt
            })
            .GetByDataQueryAsync(q);
    }

    public async Task<MarketItemDto> CreateOrUpdateMarketItem(CreateOrUpdateMarketItemDto dto)
    {
        var item = dto.Id.HasValue
            ? await dbContext.MarketItems.FirstOrDefaultAsync(x => x.Id == dto.Id.Value)
              ?? throw new MarketItemNotFoundException()
            : dbContext.MarketItems.Add(new MarketItem()).Entity;

        item.Title = dto.Title;
        item.Subtitle = dto.Subtitle;
        item.PriceCoins = dto.PriceCoins;
        item.Category = dto.Category;
        item.RewardType = dto.RewardType;
        item.RewardValue = dto.RewardValue;
        item.IsPopular = dto.IsPopular;
        item.IsActive = dto.IsActive;
        item.SortOrder = dto.SortOrder;

        await dbContext.SaveChangesAsync();

        return new MarketItemDto
        {
            Id = item.Id,
            Title = item.Title,
            Subtitle = item.Subtitle,
            PriceCoins = item.PriceCoins,
            Category = item.Category,
            RewardType = item.RewardType,
            RewardValue = item.RewardValue,
            IsPopular = item.IsPopular,
            IsActive = item.IsActive,
            SortOrder = item.SortOrder
        };
    }

    /// <summary>Xarid qilingan mahsulotni o'chirib bo'lmaydi — faolsizlantiring.</summary>
    public async Task DeleteMarketItem(long marketItemId)
    {
        if (await dbContext.MarketPurchases.AnyAsync(x => x.MarketItemId == marketItemId))
            throw new MarketItemInUseException();

        var deleted = await dbContext.MarketItems
            .Where(x => x.Id == marketItemId)
            .ExecuteDeleteAsync();

        if (deleted == 0)
            throw new MarketItemNotFoundException();
    }

    private async Task<string> GenerateUniqueCouponCode()
    {
        while (true)
        {
            var code = CodeGenerator.Generate("CC-", 8);
            if (!await dbContext.Coupons.AnyAsync(x => x.Code == code))
                return code;
        }
    }

    private static MarketPurchaseDto ToDto(MarketPurchase x) => new()
    {
        Id = x.Id,
        MarketItemId = x.MarketItemId,
        Title = x.Title,
        PriceCoins = x.PriceCoins,
        RewardType = x.RewardType,
        RewardValue = x.RewardValue,
        Code = x.Code,
        CreatedAt = x.CreatedAt
    };

    #endregion

    #region Ranking

    /// <summary>
    /// Coin reytingi: davr ichida ishlab topilgan (kirim) coinlar bo'yicha.
    /// Davr berilmasa — butun vaqt. Javob shakli <c>users/steps/stat</c> bilan bir xil.
    /// </summary>
    public async Task<Wrapper> Ranking(DateTime? from, DateTime? to, DataQueryRequest q)
    {
        from ??= new DateTime(2000, 1, 1);
        to ??= DateTime.Now.AddDays(1);

        return await dbContext.UserCoinStats
            .FromSql($@"
select sub.user_id, sub.sum, ROW_NUMBER() OVER (ORDER BY sub.sum desc, sub.user_id) as index from (
select t.user_id, sum(t.amount)::bigint as sum from coin_transactions t
where t.amount > 0 and t.created_at >= {from} and t.created_at <= {to}
group by t.user_id
) sub
")
            .LeftJoin2(dbContext.UserExtras, stat => stat.UserId, extra => extra.UserId, (x, extra) =>
                new GetCoinStatDto
                {
                    User = new UserDto(
                        x.User.Id,
                        x.User.Name,
                        x.User.Email,
                        extra != null ? new ExtraDto(extra.Photo, extra.ActivityLevel) : null
                    ),
                    Sum = x.Sum,
                    Index = x.Index
                })
            .OrderBy(x => x.Index)
            .GetByDataQueryAsync(q);
    }

    #endregion
}
