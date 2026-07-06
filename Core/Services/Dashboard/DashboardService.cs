using BRB.Core.Common.Extensions;
using BRB.Core.Common.Models;
using BRB.Core.EF.Attributes;
using BRB.Core.EF.Extensions;
using Core.Brokers.DbContext;
using Core.Entities.Billing.Enum;
using Core.Services.Dashboard.Contracts;
using Microsoft.EntityFrameworkCore;
using ResultWrapper.Library;

namespace Core.Services.Dashboard;

[Injectable]
public class DashboardService(AppDbContext context)
{
    public async Task<Dictionary<int, double>> GetSalesMonthlySummary()
    {
        var yearBegin = DateTime.Now.FirstDayOfYear();
        // Group + sum in SQL, then round client-side (Math.Round inside the aggregate isn't translatable).
        var rows = await context.Orders
            .Where(x => x.CreatedAt >= yearBegin)
            .GroupBy(x => x.CreatedAt.Month)
            .Select(g => new { Month = g.Key, Total = g.Sum(y => y.Amount) })
            .ToListAsync();

        return rows
            .OrderBy(r => r.Month)
            .ToDictionary(r => r.Month, r => Math.Round(r.Total / 100d, 2));
    }

    public async Task<GetOverallSummaryDto> GetOverallSummary()
    {
        var yesterdayStart = DateTime.Now.AddDays(-1).Date;
        var todayStart = DateTime.Now.Date;
        var tomorrowStart = DateTime.Now.AddDays(1).Date;

        var totalUsers = await context.Users.CountAsync();
        var usersGrowRatePercent =
            Math.Round(
                (await context.Users.CountAsync(x => x.CreatedAt >= todayStart && x.CreatedAt < tomorrowStart) * 1d /
                    Math.Max(
                        await context.Users.CountAsync(x => x.CreatedAt >= yesterdayStart && x.CreatedAt < todayStart),
                        1) - 1) *
                100, 2);

        var salesQuery = context.Orders
            .Where(x => x.Status == EnumOrderStatus.Confirmed);

        var totalSalesCount = await salesQuery
            .CountAsync();

        var salesCountGrowRatePercent =
            Math.Round(
                (await salesQuery.CountAsync(x => x.UpdatedAt >= todayStart && x.UpdatedAt < tomorrowStart) * 1d /
                    Math.Max(
                        await salesQuery.CountAsync(x => x.UpdatedAt >= yesterdayStart && x.UpdatedAt < todayStart),
                        1) - 1) *
                100, 2);

        var totalSalesAmount = await salesQuery
            .SumAsync(x => x.Amount);

        var salesAmountGrowRatePercent =
            Math.Round(
                (await salesQuery.Where(x => x.UpdatedAt >= todayStart && x.UpdatedAt < tomorrowStart)
                        .SumAsync(x => x.Amount) * 1d /
                    Math.Max(await salesQuery.Where(x => x.UpdatedAt >= yesterdayStart && x.UpdatedAt < todayStart)
                        .SumAsync(x => x.Amount) * 1d, 1) - 1) *
                100, 2);

        return new GetOverallSummaryDto
        {
            TotalUsers = totalUsers, TotalUsersGrows = usersGrowRatePercent, TotalSalesCount = totalSalesCount,
            TotalSalesCountGrows = salesCountGrowRatePercent,
            TotalSalesAmount = totalSalesAmount,
            TotalSalesAmountGrows = salesAmountGrowRatePercent
        };
    }

    public async Task<Wrapper> GetSubscriptionOrders(DataQueryRequest query)
    {
        return await context
            .SubscriptionOrders
            .Select(x => new GetSubscriptionOrdersDto
            {
                Id = x.Id, UserId = x.Order.UserId, UserName = x.Order.User.Name,
                Plan = x.Plan,
                PlanExtraId = x.PlanExtra.Id,
                PlanExtraDurationInMonths = x.PlanExtra.DurationInMonths,
                OrderStatus = x.Order.Status,
                Amount = Math.Round(x.Order.Amount / 100d, 2),
                CreatedAt = x.Order.CreatedAt,
                PaymentProvider = x.Order.Provider,
                Coupon = x.Order.Coupon != null ? new CouponDto { Id = x.Order.Coupon.Id, Code = x.Order.Coupon.Code } : null
            })
            .GetByDataQueryAsync(query);
    }
}