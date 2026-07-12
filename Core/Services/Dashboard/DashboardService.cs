using BRB.Core.Common.Extensions;
using BRB.Core.Common.Models;
using BRB.Core.EF.Attributes;
using BRB.Core.EF.Extensions;
using Core.Brokers.DbContext;
using Core.Entities.Billing.Enum;
using Core.Enums;
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
        // Confirmed savdolar bo'yicha — "Savdo" bo'limidagi ko'rsatkichlar bilan mos bo'lishi uchun.
        var rows = await context.Orders
            .Where(x => x.Status == EnumOrderStatus.Confirmed && x.CreatedAt >= yearBegin)
            .GroupBy(x => x.CreatedAt.Month)
            .Select(g => new { Month = g.Key, Total = g.Sum(y => y.Amount) })
            .ToListAsync();

        // Har 12 oy uchun to'ldirilgan (bo'sh oylar 0) — frontendda oy yorliqlari
        // bilan aniq moslashishi uchun.
        return Enumerable.Range(1, 12)
            .ToDictionary(
                m => m,
                m => Math.Round((rows.FirstOrDefault(r => r.Month == m)?.Total ?? 0) / 100d, 2));
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

    public async Task<GetUserStatisticsDto> GetUserStatistics()
    {
        var now = DateTime.Now;
        var todayStart = now.Date;
        var tomorrowStart = todayStart.AddDays(1);
        var yesterdayStart = todayStart.AddDays(-1);

        // Joriy hafta dushanbadan boshlanadi
        var weekOffset = ((int)now.DayOfWeek + 6) % 7;
        var weekStart = todayStart.AddDays(-weekOffset);
        var prevWeekStart = weekStart.AddDays(-7);

        var monthStart = new DateTime(now.Year, now.Month, 1);
        var prevMonthStart = monthStart.AddMonths(-1);

        var yearBegin = new DateTime(now.Year, 1, 1);
        var last30Start = todayStart.AddDays(-29);

        // Faollik uchun aylanma (rolling) oynalar
        var week7Start = now.AddDays(-7);
        var month30Start = now.AddDays(-30);

        // ── Registratsiya sanoqlari ──────────────────────────────────
        var totalUsers = await context.Users.CountAsync();

        var newToday = await context.Users.CountAsync(x => x.CreatedAt >= todayStart && x.CreatedAt < tomorrowStart);
        var newYesterday = await context.Users.CountAsync(x => x.CreatedAt >= yesterdayStart && x.CreatedAt < todayStart);

        var newThisWeek = await context.Users.CountAsync(x => x.CreatedAt >= weekStart);
        var newPrevWeek = await context.Users.CountAsync(x => x.CreatedAt >= prevWeekStart && x.CreatedAt < weekStart);

        var newThisMonth = await context.Users.CountAsync(x => x.CreatedAt >= monthStart);
        var newPrevMonth = await context.Users.CountAsync(x => x.CreatedAt >= prevMonthStart && x.CreatedAt < monthStart);

        // ── Faollik (SignLog: ilovaga kirganlar) ─────────────────────
        var activeToday = await context.SignLogs
            .Where(x => x.SignAt >= todayStart).Select(x => x.UserId).Distinct().CountAsync();
        var activeThisWeek = await context.SignLogs
            .Where(x => x.SignAt >= week7Start).Select(x => x.UserId).Distinct().CountAsync();
        var activeThisMonth = await context.SignLogs
            .Where(x => x.SignAt >= month30Start).Select(x => x.UserId).Distinct().CountAsync();

        // ── Obuna kesimi (faol obunalar) ─────────────────────────────
        var activeSubs = context.Subscriptions.Where(x => x.IsActive && x.EndsAt > now);
        var premiumCount = await activeSubs.CountAsync(x => x.SubscriptionPlan == EnumSPlans.Premium);
        var proCount = await activeSubs.CountAsync(x => x.SubscriptionPlan == EnumSPlans.Pro);
        var premiumUsers = premiumCount + proCount;
        var freeUsers = Math.Max(totalUsers - premiumUsers, 0);

        var planBreakdown = new List<PlanBreakdownDto>
        {
            new() { Plan = EnumSPlans.Free, Count = freeUsers },
            new() { Plan = EnumSPlans.Premium, Count = premiumCount },
            new() { Plan = EnumSPlans.Pro, Count = proCount },
        };

        // ── Kunlik registratsiya trendi (oxirgi 30 kun) ──────────────
        var regRows = await context.Users
            .Where(x => x.CreatedAt >= last30Start)
            .GroupBy(x => x.CreatedAt.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .ToListAsync();
        var dailyRegistrations = FillDailySeries(regRows.ToDictionary(r => r.Date, r => r.Count), last30Start, todayStart);

        // ── Kunlik faol foydalanuvchilar trendi (oxirgi 30 kun) ──────
        var actRows = await context.SignLogs
            .Where(x => x.SignAt >= last30Start)
            .GroupBy(x => x.SignAt.Date)
            .Select(g => new { Date = g.Key, Count = g.Select(y => y.UserId).Distinct().Count() })
            .ToListAsync();
        var dailyActiveUsers = FillDailySeries(actRows.ToDictionary(r => r.Date, r => r.Count), last30Start, todayStart);

        // ── Oylik registratsiya (joriy yil) ──────────────────────────
        var monthRows = await context.Users
            .Where(x => x.CreatedAt >= yearBegin)
            .GroupBy(x => x.CreatedAt.Month)
            .Select(g => new { Month = g.Key, Count = g.Count() })
            .ToListAsync();
        var monthlyRegistrations = Enumerable.Range(1, 12)
            .ToDictionary(m => m, m => monthRows.FirstOrDefault(r => r.Month == m)?.Count ?? 0);

        return new GetUserStatisticsDto
        {
            TotalUsers = totalUsers,
            NewToday = newToday,
            NewThisWeek = newThisWeek,
            NewThisMonth = newThisMonth,
            NewTodayGrows = GrowthPercent(newToday, newYesterday),
            NewThisWeekGrows = GrowthPercent(newThisWeek, newPrevWeek),
            NewThisMonthGrows = GrowthPercent(newThisMonth, newPrevMonth),
            ActiveToday = activeToday,
            ActiveThisWeek = activeThisWeek,
            ActiveThisMonth = activeThisMonth,
            PremiumUsers = premiumUsers,
            FreeUsers = freeUsers,
            PlanBreakdown = planBreakdown,
            DailyRegistrations = dailyRegistrations,
            DailyActiveUsers = dailyActiveUsers,
            MonthlyRegistrations = monthlyRegistrations,
        };
    }

    private static List<DailyCountDto> FillDailySeries(
        IReadOnlyDictionary<DateTime, int> counts, DateTime from, DateTime to)
    {
        var series = new List<DailyCountDto>();
        for (var day = from; day <= to; day = day.AddDays(1))
            series.Add(new DailyCountDto { Date = day, Count = counts.GetValueOrDefault(day, 0) });
        return series;
    }

    private static double GrowthPercent(double current, double previous) =>
        Math.Round((current / Math.Max(previous, 1) - 1) * 100, 2);

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