using BRB.Core.Common.Models;
using BRB.Core.EF.Attributes;
using BRB.Core.EF.Extensions;
using Core.Brokers.DbContext;
using Core.Entities.Coins.Enum;
using Core.Helpers;
using Core.Services.Coins.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ResultWrapper.Library;

namespace Core.Services.Coins;

/// <summary>
/// Dashboard: coin do'koni (tariflar narxi, faolligi) va sotuv statistikasi.
/// Mahsulotni saqlash/o'chirish <see cref="CoinService"/>da (mobile admin endpointlari bilan bir xil qoida).
/// </summary>
[Injectable]
public class MarketAdminService(
    AppDbContext dbContext,
    CoinRuleService coinRuleService,
    IOptions<CoinConfig> options)
{
    /// <summary>Kunlik grafik uchun eng ko'p kun.</summary>
    private const int MaxDays = 366;

    private (DateTime From, DateTime To) Period(DateTime? from, DateTime? to) =>
        DashboardPeriod.Resolve(from, to, options.Value.CoinsEarnStartDate);

    /// <summary>Mobile do'kon faqat faol Premium tariflarni ko'rsatadi.</summary>
    private static bool VisibleInApp(bool isActive, EnumMarketCategory category, EnumMarketRewardType rewardType) =>
        isActive && category == EnumMarketCategory.Tariff && rewardType == EnumMarketRewardType.PremiumDays;

    public async Task<List<AdminMarketItemDto>> GetItems(DateTime? from, DateTime? to)
    {
        var (start, end) = Period(from, to);

        var items = await dbContext.MarketItems
            .AsNoTracking()
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.PriceCoins)
            .ToListAsync();

        var totals = await dbContext.MarketPurchases
            .GroupBy(x => x.MarketItemId)
            .Select(g => new { ItemId = g.Key, Count = g.Count(), Last = g.Max(x => x.CreatedAt) })
            .ToDictionaryAsync(x => x.ItemId);

        var inPeriod = await dbContext.MarketPurchases
            .Where(x => x.CreatedAt >= start && x.CreatedAt <= end)
            .GroupBy(x => x.MarketItemId)
            .Select(g => new
            {
                ItemId = g.Key,
                Count = g.Count(),
                Coins = g.Sum(x => x.PriceCoins),
                Buyers = g.Select(x => x.UserId).Distinct().Count()
            })
            .ToDictionaryAsync(x => x.ItemId);

        var perDay = await coinRuleService.AvgCoinsPerEarningDay();

        return items.Select(x =>
        {
            var total = totals.GetValueOrDefault(x.Id);
            var period = inPeriod.GetValueOrDefault(x.Id);
            var isPremium = x.RewardType == EnumMarketRewardType.PremiumDays && x.RewardValue > 0;

            return new AdminMarketItemDto
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
                SortOrder = x.SortOrder,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt,
                VisibleInApp = VisibleInApp(x.IsActive, x.Category, x.RewardType),
                TotalPurchases = total?.Count ?? 0,
                LastPurchaseAt = total?.Last,
                Purchases = period?.Count ?? 0,
                CoinsSpent = period?.Coins ?? 0,
                Buyers = period?.Buyers ?? 0,
                DaysToEarn = perDay > 0 ? Math.Round(x.PriceCoins / perDay, 1) : null,
                CoinsPerPremiumDay = isPremium ? Math.Round(x.PriceCoins / (double)x.RewardValue, 1) : null
            };
        }).ToList();
    }

    public async Task<AdminMarketSummaryDto> GetSummary(DateTime? from, DateTime? to)
    {
        var (start, end) = Period(from, to);

        var purchases = await dbContext.MarketPurchases
            .AsNoTracking()
            .Where(x => x.CreatedAt >= start && x.CreatedAt <= end)
            .Select(x => new { x.UserId, x.PriceCoins, x.RewardType, x.RewardValue, x.CreatedAt })
            .ToListAsync();

        var cheapest = await dbContext.MarketItems
            .Where(x => x.IsActive && x.Category == EnumMarketCategory.Tariff &&
                        x.RewardType == EnumMarketRewardType.PremiumDays)
            .OrderBy(x => x.PriceCoins)
            .Select(x => (long?)x.PriceCoins)
            .FirstOrDefaultAsync();

        var canAfford = cheapest is null
            ? 0
            : await dbContext.CoinWallets.CountAsync(x => !x.User.IsDeleted && x.Balance >= cheapest);

        var circulation = await dbContext.CoinWallets
            .Where(x => !x.User.IsDeleted)
            .SumAsync(x => x.Balance);

        var activeItems = await dbContext.MarketItems.CountAsync(x => x.IsActive);
        var rule = (await coinRuleService.GetRuleSet()).Today;

        var byDay = purchases
            .GroupBy(x => x.CreatedAt.Date)
            .ToDictionary(g => g.Key, g => new { Count = g.Count(), Coins = g.Sum(x => x.PriceCoins) });

        var lastDay = end.Date < DateTime.Now.Date ? end.Date : DateTime.Now.Date;
        var firstDay = start.Date;
        if ((lastDay - firstDay).Days >= MaxDays)
            firstDay = lastDay.AddDays(1 - MaxDays);

        var days = new List<AdminMarketDayDto>();
        for (var d = firstDay; d <= lastDay; d = d.AddDays(1))
        {
            var day = byDay.GetValueOrDefault(d);
            days.Add(new AdminMarketDayDto { Date = d, Purchases = day?.Count ?? 0, CoinsSpent = day?.Coins ?? 0 });
        }

        return new AdminMarketSummaryDto
        {
            From = start,
            To = end,
            Purchases = purchases.Count,
            Buyers = purchases.Select(x => x.UserId).Distinct().Count(),
            CoinsSpent = purchases.Sum(x => x.PriceCoins),
            PremiumDaysGranted = purchases
                .Where(x => x.RewardType == EnumMarketRewardType.PremiumDays)
                .Sum(x => x.RewardValue),
            RepeatBuyers = purchases.GroupBy(x => x.UserId).Count(g => g.Count() > 1),
            ActiveItems = activeItems,
            CheapestPrice = cheapest,
            CanAffordCheapest = canAfford,
            BalanceInCirculation = circulation,
            AvgCoinsPerEarningDay = await coinRuleService.AvgCoinsPerEarningDay(),
            StepsPerCoin = rule.StepsPerCoin,
            MaxDailyCoins = rule.MaxDailyCoins,
            Days = days
        };
    }

    /// <summary>Xaridlar (yangidan eskiga). <c>search</c> — ism, email, telefon yoki user id.</summary>
    public async Task<Wrapper> GetPurchases(DateTime? from, DateTime? to, long? itemId, string? search,
        DataQueryRequest q)
    {
        var (start, end) = Period(from, to);

        var query = dbContext.MarketPurchases
            .AsNoTracking()
            .Where(x => !x.User.IsDeleted)
            .Where(x => x.CreatedAt >= start && x.CreatedAt <= end)
            .Where(x => itemId == null || x.MarketItemId == itemId);

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
        var rows = await query
            .OrderByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.Id)
            .Page(q)
            .Select(x => new AdminMarketPurchaseDto
            {
                Id = x.Id,
                CreatedAt = x.CreatedAt,
                UserId = x.UserId,
                Name = x.User.Name,
                Email = x.User.Email,
                Phone = x.User.Phone,
                Photo = dbContext.UserExtras
                    .Where(e => e.UserId == x.UserId)
                    .Select(e => e.Photo)
                    .FirstOrDefault(),
                MarketItemId = x.MarketItemId,
                Title = x.Title,
                PriceCoins = x.PriceCoins,
                RewardType = x.RewardType,
                RewardValue = x.RewardValue,
                Code = x.Code
            })
            .ToListAsync();

        return (rows, total);
    }
}
