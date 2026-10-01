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
using Core.Services.User.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ResultWrapper.Library;

namespace Core.Services.Coins;

/// <summary>
/// Coin hamyoni: qadamlardan avtomatik yig'iladigan coinlar (har N qadam = 1 coin, kunlik limit bilan —
/// qoidalar <see cref="CoinRuleService"/>da, dashboard'dan boshqariladi), marketplace xaridlari,
/// tarix va coin reytingi. Balans o'zgarishlari atomik (<c>ExecuteUpdate</c> / qatorni qulflash) —
/// parallel so'rovlarda manfiy balans yoki ikki marta berilgan coin bo'lmaydi.
/// </summary>
[Injectable]
public class CoinService(
    AppDbContext dbContext,
    SubscriptionService subscriptionService,
    AiQuotaService aiQuotaService,
    CoinRuleService coinRuleService,
    IOptions<CoinConfig> options)
{
    private const string DailyStepsTitle = "coin_tx_daily_steps";
    private CoinConfig Config => options.Value;

    #region Wallet

    public async Task<WalletDto> GetWallet(long userId)
    {
        await SyncStepCoins(userId);

        var rules = await coinRuleService.GetRuleSet();
        var rule = rules.Today;
        var next = rules.Next;

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
            StepsPerCoin = rule.StepsPerCoin,
            MaxDailyCoins = rule.MaxDailyCoins,
            NextRule = next is null
                ? null
                : new CoinRuleBriefDto
                {
                    StepsPerCoin = next.StepsPerCoin,
                    MaxDailyCoins = next.MaxDailyCoins,
                    EffectiveFrom = next.EffectiveFrom
                }
        };
    }

    public async Task<Wrapper> GetTransactions(long userId, DataQueryRequest q, EnumCoinTxType? type = null)
    {
        var query = dbContext.CoinTransactions
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .Where(x => type == null || x.Type == type);

        var total = await query.CountAsync();
        var rows = await query
            .OrderByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.Id)
            .Page(q)
            .Select(x => new { x.Id, x.Title, x.Amount, x.Type, x.CreatedAt, x.RefId })
            .ToListAsync();

        // Qadam coini kechroq yozilishi mumkin (masalan, ilova ochilganda bir necha kun
        // uchun birdan) — tarixda coin qaysi kun va nechta qadam uchun ekani ko'rinsin.
        DateTime? StepDay(EnumCoinTxType t, long? refId) =>
            t == EnumCoinTxType.Steps && refId is > 0 ? FromDayRef(refId.Value) : null;

        var stepDays = rows
            .Select(x => StepDay(x.Type, x.RefId))
            .OfType<DateTime>()
            .ToList();

        var stepsByDay = new Dictionary<DateTime, long>();
        if (stepDays.Count > 0)
        {
            var from = stepDays.Min();
            var to = stepDays.Max().AddDays(1);
            stepsByDay = (await dbContext.UserDailies
                    .AsNoTracking()
                    .Where(x => x.UserId == userId && x.Metric == EnumMetrics.Step &&
                                x.Date >= from && x.Date < to)
                    .Select(x => new { x.Date, x.Value })
                    .ToListAsync())
                .GroupBy(x => x.Date.Date)
                .ToDictionary(g => g.Key, g => (long)g.Max(x => x.Value));
        }

        var result = rows.Select(x =>
        {
            var day = StepDay(x.Type, x.RefId);
            return new CoinTransactionDto
            {
                Id = x.Id,
                Title = x.Title,
                Amount = x.Amount,
                Type = x.Type,
                CreatedAt = x.CreatedAt,
                StepDate = day,
                Steps = day.HasValue && stepsByDay.TryGetValue(day.Value, out var s) ? s : null
            };
        }).ToList();

        return (result, total);
    }

    /// <summary>
    /// Qadamlar uchun coinlarni hamyonga yozadi: har kun uchun
    /// <c>min(qadam / StepsPerCoin, MaxDailyCoins)</c> coin (o'sha kunda amal qilgan qoida bo'yicha —
    /// yangi qoida o'tgan kunlarga orqaga qarab coin qo'shmaydi), kuniga bitta tarix yozuvi.
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

        if (createdAt is null)
            return;

        var start = createdAt.Value.Date > Config.CoinsEarnStartDate.Date
            ? createdAt.Value.Date
            : Config.CoinsEarnStartDate.Date;

        var days = await dbContext.UserDailies
            .AsNoTracking()
            .Where(x => x.UserId == userId && x.Metric == EnumMetrics.Step && x.Date >= start)
            .Select(x => new { x.Date, x.Value })
            .ToListAsync();

        var rules = await coinRuleService.GetRuleSet();

        var expected = days
            .GroupBy(x => x.Date.Date)
            .Select(g => new
            {
                Day = g.Key,
                Coins = rules.CoinsFor(g.Key, g.Max(x => x.Value))
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

    private static DateTime FromDayRef(long dayRef) =>
        new((int)(dayRef / 10000), (int)(dayRef / 100 % 100), (int)(dayRef % 100));

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

    /// <summary>
    /// Do'kon mahsulotini yaratadi/yangilaydi. Premium tarif kamida 1 kun bo'lishi kerak.
    /// "Mashhur" belgisi bitta bo'ladi — shu kategoriyadagi boshqa mahsulotlardan olib tashlanadi.
    /// </summary>
    public async Task<MarketItemDto> CreateOrUpdateMarketItem(CreateOrUpdateMarketItemDto dto)
    {
        if (dto.RewardType == EnumMarketRewardType.PremiumDays && dto.RewardValue is < 1 or > 3650)
            throw new MarketItemInvalidException();

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

        await dbContext.Transactional(async () =>
        {
            await dbContext.SaveChangesAsync();

            if (item.IsPopular)
                await dbContext.MarketItems
                    .Where(x => x.Id != item.Id && x.Category == item.Category && x.IsPopular)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(x => x.IsPopular, false)
                        .SetProperty(x => x.UpdatedAt, DateTime.Now));
        });

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
        return await RankedEarnings(from ?? new DateTime(2000, 1, 1), to ?? DateTime.Now.AddDays(1))
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

    /// <summary>
    /// Davrda ishlab topilgan coinlar bo'yicha reyting (<see cref="UserCoinStat.Index"/> 1 dan).
    /// Qadam coini qadam kuni (<c>ref_id</c>) bo'yicha hisoblanadi, yozilgan vaqti bo'yicha emas —
    /// bir necha kunlik qadam birdan sinxronlansa ham har kun o'z davriga tushadi. Boshqa kirimlar
    /// (referral, admin) — yozilgan vaqti bo'yicha. O'chirilgan userlar reytingga kirmaydi.
    /// Filtr <see cref="EarnedInPeriod"/> bilan bir xil.
    /// </summary>
    private IQueryable<UserCoinStat> RankedEarnings(DateTime from, DateTime to)
    {
        var steps = (int)EnumCoinTxType.Steps;
        var fromRef = DayRef(from.Date);
        var toRef = DayRef(to.Date);

        return dbContext.UserCoinStats
            .FromSql($@"
select sub.user_id, sub.sum, ROW_NUMBER() OVER (ORDER BY sub.sum desc, sub.user_id) as index from (
select t.user_id, sum(t.amount)::bigint as sum from coin_transactions t
join users u on u.id = t.user_id and not u.is_deleted
where t.amount > 0 and (
    (t.type = {steps} and t.ref_id >= {fromRef} and t.ref_id <= {toRef}) or
    (t.type <> {steps} and t.created_at >= {from} and t.created_at <= {to}))
group by t.user_id
) sub
");
    }

    /// <summary>Davrdagi kirim yozuvlari — <see cref="RankedEarnings"/> filtri, LINQ uchun.</summary>
    private IQueryable<CoinTransaction> EarnedInPeriod(DateTime from, DateTime to)
    {
        var fromRef = DayRef(from.Date);
        var toRef = DayRef(to.Date);

        return dbContext.CoinTransactions
            .AsNoTracking()
            .Where(x => x.Amount > 0 && !x.User.IsDeleted)
            .Where(x => (x.Type == EnumCoinTxType.Steps && x.RefId >= fromRef && x.RefId <= toRef) ||
                        (x.Type != EnumCoinTxType.Steps && x.CreatedAt >= from && x.CreatedAt <= to));
    }

    /// <summary>Kirim qaysi kunga tegishli: qadam coini — qadam kuni, qolganlari — yozilgan kun.</summary>
    private static DateTime EarnDay(EnumCoinTxType type, long? refId, DateTime createdAt) =>
        type == EnumCoinTxType.Steps && refId is > 0 ? FromDayRef(refId.Value) : createdAt.Date;

    #endregion

    #region Admin (dashboard)

    /// <summary>Dashboard'dagi kunlik grafik/jadval uchun eng ko'p kun.</summary>
    private const int MaxAdminDays = 366;

    /// <summary>
    /// Dashboard davri: <paramref name="from"/> kun boshidan <paramref name="to"/> kun oxirigacha
    /// (ikkala kun ham kiradi). Berilmasa — coin ishga tushgan kundan bugungacha.
    /// </summary>
    private (DateTime From, DateTime To) AdminPeriod(DateTime? from, DateTime? to)
    {
        var start = (from ?? Config.CoinsEarnStartDate).Date;
        var end = (to ?? DateTime.Now).Date;
        if (end < start) (start, end) = (end, start);

        return (start, end.AddDays(1).AddTicks(-10));
    }

    public async Task<AdminCoinSummaryDto> AdminSummary(DateTime? from, DateTime? to)
    {
        var (start, end) = AdminPeriod(from, to);

        var earned = await EarnedInPeriod(start, end)
            .GroupBy(x => x.Type == EnumCoinTxType.Steps)
            .Select(g => new { IsSteps = g.Key, Sum = g.Sum(x => x.Amount) })
            .ToListAsync();

        var participants = await EarnedInPeriod(start, end)
            .Select(x => x.UserId)
            .Distinct()
            .CountAsync();

        var spent = await dbContext.CoinTransactions
            .Where(x => x.Amount < 0 && x.CreatedAt >= start && x.CreatedAt <= end)
            .SumAsync(x => -x.Amount);

        var circulation = await dbContext.CoinWallets
            .Where(x => !x.User.IsDeleted)
            .SumAsync(x => x.Balance);

        var stepCoins = earned.Where(x => x.IsSteps).Sum(x => x.Sum);
        var bonusCoins = earned.Where(x => !x.IsSteps).Sum(x => x.Sum);
        var rule = (await coinRuleService.GetRuleSet()).Today;

        return new AdminCoinSummaryDto
        {
            From = start,
            To = end,
            Participants = participants,
            Earned = stepCoins + bonusCoins,
            StepCoins = stepCoins,
            BonusCoins = bonusCoins,
            Spent = spent,
            AvgPerParticipant = participants > 0 ? Math.Round((double)(stepCoins + bonusCoins) / participants, 1) : 0,
            BalanceInCirculation = circulation,
            StepsPerCoin = rule.StepsPerCoin,
            MaxDailyCoins = rule.MaxDailyCoins
        };
    }

    /// <summary>
    /// Dashboard coin reytingi: davrda ishlab topilgan coinlar, hamyon holati va faol kunlar.
    /// <paramref name="search"/> (ism, email, telefon yoki user id) faqat ro'yxatni filtrlaydi —
    /// <see cref="AdminCoinRankingDto.Rank"/> umumiy reytingdagi o'rin bo'lib qoladi.
    /// </summary>
    public async Task<Wrapper> AdminRanking(DateTime? from, DateTime? to, string? search, DataQueryRequest q)
    {
        var (start, end) = AdminPeriod(from, to);
        var query = RankedEarnings(start, end);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            var pattern = $"%{term}%";
            var id = long.TryParse(term, out var parsed) ? parsed : 0;

            query = query.Where(x => x.UserId == id ||
                                     EF.Functions.ILike(x.User.Name, pattern) ||
                                     EF.Functions.ILike(x.User.Email!, pattern) ||
                                     EF.Functions.ILike(x.User.Phone!, pattern));
        }

        var total = await query.CountAsync();
        var page = await query
            .OrderBy(x => x.Index)
            .Page(q)
            .Select(x => new { x.UserId, x.Sum, x.Index, x.User.Name, x.User.Email, x.User.Phone })
            .ToListAsync();

        var ids = page.Select(x => x.UserId).ToList();
        var rules = await coinRuleService.GetRuleSet();

        var photos = (await dbContext.UserExtras
                .AsNoTracking()
                .Where(x => ids.Contains(x.UserId))
                .Select(x => new { x.UserId, x.Photo })
                .ToListAsync())
            .DistinctBy(x => x.UserId)
            .ToDictionary(x => x.UserId, x => x.Photo);

        var wallets = await dbContext.CoinWallets
            .AsNoTracking()
            .Where(x => ids.Contains(x.UserId))
            .Select(x => new { x.UserId, x.Balance, x.TotalEarned, x.TotalSpent })
            .ToDictionaryAsync(x => x.UserId);

        var stats = (await EarnedInPeriod(start, end)
                .Where(x => ids.Contains(x.UserId))
                .Select(x => new { x.UserId, x.Type, x.RefId, x.Amount, x.CreatedAt, x.UpdatedAt })
                .ToListAsync())
            .GroupBy(x => x.UserId)
            .ToDictionary(g => g.Key, g => new
            {
                StepCoins = g.Where(x => x.Type == EnumCoinTxType.Steps).Sum(x => x.Amount),
                BonusCoins = g.Where(x => x.Type != EnumCoinTxType.Steps).Sum(x => x.Amount),
                ActiveDays = g.Select(x => EarnDay(x.Type, x.RefId, x.CreatedAt)).Distinct().Count(),
                MaxedDays = g.Count(x => x.Type == EnumCoinTxType.Steps &&
                                         x.Amount >= rules.For(EarnDay(x.Type, x.RefId, x.CreatedAt)).MaxDailyCoins),
                LastEarnedAt = g.Max(x => x.UpdatedAt)
            });

        var result = page.Select(x =>
        {
            var wallet = wallets.GetValueOrDefault(x.UserId);
            var stat = stats.GetValueOrDefault(x.UserId);

            return new AdminCoinRankingDto
            {
                Rank = x.Index,
                UserId = x.UserId,
                Name = x.Name,
                Email = x.Email,
                Phone = x.Phone,
                Photo = photos.GetValueOrDefault(x.UserId),
                Earned = x.Sum,
                StepCoins = stat?.StepCoins ?? 0,
                BonusCoins = stat?.BonusCoins ?? 0,
                ActiveDays = stat?.ActiveDays ?? 0,
                MaxedDays = stat?.MaxedDays ?? 0,
                Balance = wallet?.Balance ?? 0,
                TotalEarned = wallet?.TotalEarned ?? 0,
                TotalSpent = wallet?.TotalSpent ?? 0,
                LastEarnedAt = stat?.LastEarnedAt
            };
        }).ToList();

        return (result, total);
    }

    /// <summary>
    /// Bitta userning coinlari: hamyon, davr reytingidagi o'rni va har bir kun uchun
    /// qadam / qadam coini / bonus / sarf (davrdagi har kun, coinsiz kunlar ham).
    /// </summary>
    public async Task<AdminUserCoinsDto> AdminUserCoins(long userId, DateTime? from, DateTime? to)
    {
        var user = await dbContext.Users
                       .AsNoTracking()
                       .Where(x => x.Id == userId)
                       .Select(x => new
                       {
                           x.Id, x.Name, x.Email, x.Phone, x.CreatedAt,
                           Photo = x.Extra != null ? x.Extra.Photo : null
                       })
                       .FirstOrDefaultAsync()
                   ?? throw new UserNotFoundException();

        var (start, end) = AdminPeriod(from, to);

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

        var earned = await EarnedInPeriod(start, end)
            .Where(x => x.UserId == userId)
            .Select(x => new { x.Type, x.RefId, x.Amount, x.CreatedAt })
            .ToListAsync();

        var spentByDay = (await dbContext.CoinTransactions
                .AsNoTracking()
                .Where(x => x.UserId == userId && x.Amount < 0 && x.CreatedAt >= start && x.CreatedAt <= end)
                .Select(x => new { x.Amount, x.CreatedAt })
                .ToListAsync())
            .GroupBy(x => x.CreatedAt.Date)
            .ToDictionary(g => g.Key, g => -g.Sum(x => x.Amount));

        var stepsByDay = (await dbContext.UserDailies
                .AsNoTracking()
                .Where(x => x.UserId == userId && x.Metric == EnumMetrics.Step &&
                            x.Date >= start && x.Date <= end)
                .Select(x => new { x.Date, x.Value })
                .ToListAsync())
            .GroupBy(x => x.Date.Date)
            .ToDictionary(g => g.Key, g => (long)g.Max(x => x.Value));

        var stepCoinsByDay = earned
            .Where(x => x.Type == EnumCoinTxType.Steps)
            .GroupBy(x => EarnDay(x.Type, x.RefId, x.CreatedAt))
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Amount));

        var bonusByDay = earned
            .Where(x => x.Type != EnumCoinTxType.Steps)
            .GroupBy(x => x.CreatedAt.Date)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Amount));

        var rules = await coinRuleService.GetRuleSet();
        var todayRule = rules.Today;

        AdminCoinDayDto Day(DateTime d)
        {
            var rule = rules.For(d);
            return new AdminCoinDayDto
            {
                Date = d,
                Steps = stepsByDay.GetValueOrDefault(d),
                StepCoins = stepCoinsByDay.GetValueOrDefault(d),
                BonusCoins = bonusByDay.GetValueOrDefault(d),
                Spent = spentByDay.GetValueOrDefault(d),
                StepsPerCoin = rule.StepsPerCoin,
                MaxDailyCoins = rule.MaxDailyCoins
            };
        }

        var earnedDays = stepCoinsByDay.Keys.Union(bonusByDay.Keys).Select(Day).ToList();
        var bestDay = earnedDays
            .OrderByDescending(x => x.Earned)
            .ThenByDescending(x => x.Date)
            .FirstOrDefault();

        // Coin ishga tushishidan / ro'yxatdan o'tishdan oldingi va kelajakdagi kunlar ko'rsatilmaydi.
        var firstDay = new[] { start, user.CreatedAt.Date, Config.CoinsEarnStartDate.Date }.Max();
        var lastDay = end.Date < DateTime.Now.Date ? end.Date : DateTime.Now.Date;
        if ((lastDay - firstDay).Days >= MaxAdminDays)
            firstDay = lastDay.AddDays(1 - MaxAdminDays);

        var days = new List<AdminCoinDayDto>();
        for (var d = firstDay; d <= lastDay; d = d.AddDays(1))
            days.Add(Day(d));

        var rank = await RankedEarnings(start, end)
            .Where(x => x.UserId == userId)
            .Select(x => (int?)x.Index)
            .FirstOrDefaultAsync();

        var participants = await RankedEarnings(start, end).CountAsync();

        return new AdminUserCoinsDto
        {
            UserId = user.Id,
            Name = user.Name,
            Email = user.Email,
            Phone = user.Phone,
            Photo = user.Photo,
            RegisteredAt = user.CreatedAt,
            Balance = wallet?.Balance ?? 0,
            TotalEarned = wallet?.TotalEarned ?? 0,
            TotalSpent = wallet?.TotalSpent ?? 0,
            TodayCoins = todayCoins,
            From = start,
            To = end,
            Rank = rank,
            Participants = participants,
            Earned = earned.Sum(x => x.Amount),
            StepCoins = stepCoinsByDay.Values.Sum(),
            BonusCoins = bonusByDay.Values.Sum(),
            Spent = spentByDay.Values.Sum(),
            ActiveDays = earnedDays.Count,
            MaxedDays = stepCoinsByDay.Count(x => x.Value >= rules.For(x.Key).MaxDailyCoins),
            TotalSteps = stepsByDay.Values.Sum(),
            BestDay = bestDay,
            StepsPerCoin = todayRule.StepsPerCoin,
            MaxDailyCoins = todayRule.MaxDailyCoins,
            Days = days
        };
    }

    #endregion
}
