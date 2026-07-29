using BRB.Core.Common.Extensions;
using BRB.Core.Common.Models;
using BRB.Core.EF.Attributes;
using BRB.Core.EF.Extensions;
using Core.Brokers.DbContext;
using Core.Entities.Billing.Enum;
using Core.Enums;
using Core.Services.Dashboard.Contracts;
using Core.Services.User.Contracts;
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
            // Faqat haqiqatda to'langan sotuvlar (Amount > 0). 100% promokod bilan
            // bepul olingan premiumlar tushum hisoblanmaydi.
            .Where(x => x.Status == EnumOrderStatus.Confirmed && x.Amount > 0 && x.CreatedAt >= yearBegin)
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
        var usersToday = await context.Users.CountAsync(x => x.CreatedAt >= todayStart && x.CreatedAt < tomorrowStart);
        var usersYesterday = await context.Users.CountAsync(x => x.CreatedAt >= yesterdayStart && x.CreatedAt < todayStart);
        var usersGrowRatePercent = GrowthPercent(usersToday, usersYesterday);

        // Faqat haqiqatda to'langan sotuvlar (Amount > 0) — 100% promokod bilan
        // bepul olingan premiumlar sotuv/tushum hisoblanmaydi.
        var salesQuery = context.Orders
            .Where(x => x.Status == EnumOrderStatus.Confirmed && x.Amount > 0);

        var totalSalesCount = await salesQuery.CountAsync();

        var salesCountToday = await salesQuery.CountAsync(x => x.UpdatedAt >= todayStart && x.UpdatedAt < tomorrowStart);
        var salesCountYesterday = await salesQuery.CountAsync(x => x.UpdatedAt >= yesterdayStart && x.UpdatedAt < todayStart);
        var salesCountGrowRatePercent = GrowthPercent(salesCountToday, salesCountYesterday);

        // Amount tiyinda saqlanadi — so'mga o'tkazamiz (dashboard kartochkalari uchun).
        var totalSalesAmount = Math.Round(await salesQuery.SumAsync(x => x.Amount) / 100d, 2);

        var salesAmountToday = await salesQuery
            .Where(x => x.UpdatedAt >= todayStart && x.UpdatedAt < tomorrowStart).SumAsync(x => x.Amount);
        var salesAmountYesterday = await salesQuery
            .Where(x => x.UpdatedAt >= yesterdayStart && x.UpdatedAt < todayStart).SumAsync(x => x.Amount);
        var salesAmountGrowRatePercent = GrowthPercent(salesAmountToday, salesAmountYesterday);

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

    public async Task<GetUserStatisticsRangeDto> GetUserStatisticsRange(DateTime from, DateTime to)
    {
        // Sana chegaralari: from — kun boshidan, to — o'sha kun oxirigacha (keyingi kun 00:00 gacha).
        var fromStart = from.Date;
        var toEnd = to.Date.AddDays(1);
        if (toEnd <= fromStart) toEnd = fromStart.AddDays(1);
        var lastDay = toEnd.AddDays(-1);

        var registered = await context.Users
            .CountAsync(x => x.CreatedAt >= fromStart && x.CreatedAt < toEnd);

        var activeUsers = await context.SignLogs
            .Where(x => x.SignAt >= fromStart && x.SignAt < toEnd)
            .Select(x => x.UserId).Distinct().CountAsync();

        var signInCount = await context.SignLogs
            .CountAsync(x => x.SignAt >= fromStart && x.SignAt < toEnd);

        var premiumQuery = context.Subscriptions
            .Where(x => x.StartsAt >= fromStart && x.StartsAt < toEnd
                        && (x.SubscriptionPlan == EnumSPlans.Premium || x.SubscriptionPlan == EnumSPlans.Pro));
        var newPremium = await premiumQuery.CountAsync();

        var regRows = await context.Users
            .Where(x => x.CreatedAt >= fromStart && x.CreatedAt < toEnd)
            .GroupBy(x => x.CreatedAt.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .ToListAsync();

        var actRows = await context.SignLogs
            .Where(x => x.SignAt >= fromStart && x.SignAt < toEnd)
            .GroupBy(x => x.SignAt.Date)
            .Select(g => new { Date = g.Key, Count = g.Select(y => y.UserId).Distinct().Count() })
            .ToListAsync();

        var premRows = await premiumQuery
            .GroupBy(x => x.StartsAt.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .ToListAsync();

        return new GetUserStatisticsRangeDto
        {
            From = fromStart,
            To = lastDay,
            Registered = registered,
            ActiveUsers = activeUsers,
            SignInCount = signInCount,
            NewPremium = newPremium,
            DailyRegistrations = FillDailySeries(regRows.ToDictionary(r => r.Date, r => r.Count), fromStart, lastDay),
            DailyActiveUsers = FillDailySeries(actRows.ToDictionary(r => r.Date, r => r.Count), fromStart, lastDay),
            DailyPremium = FillDailySeries(premRows.ToDictionary(r => r.Date, r => r.Count), fromStart, lastDay),
        };
    }

    public async Task<GetAudienceAnalyticsDto> GetAudienceAnalytics()
    {
        var now = DateTime.Now;
        var totalUsers = await context.Users.CountAsync();

        // Soat/hafta kunlari kesimlarini xotirada hisoblaymiz — DateTime.Hour va
        // DayOfWeek ni SQL'ga tarjima qilish provayderga bog'liq va ishonchsiz.
        var createdAts = await context.Users
            .Select(x => x.CreatedAt)
            .ToListAsync();

        // ── Soat bo'yicha ro'yxatdan o'tishlar (0–23) ────────────────
        var hourMap = createdAts
            .GroupBy(d => d.Hour)
            .ToDictionary(g => g.Key, g => g.Count());
        var hourly = Enumerable.Range(0, 24)
            .Select(h => new HourCountDto { Hour = h, Count = hourMap.GetValueOrDefault(h, 0) })
            .ToList();
        int? peakHour = hourly.Any(h => h.Count > 0)
            ? hourly.OrderByDescending(h => h.Count).First().Hour
            : null;

        // ── Hafta kunlari bo'yicha (0=Yakshanba … 6=Shanba) ──────────
        var weekdayMap = createdAts
            .GroupBy(d => (int)d.DayOfWeek)
            .ToDictionary(g => g.Key, g => g.Count());
        var weekdays = Enumerable.Range(0, 7)
            .Select(d => new WeekdayCountDto { Weekday = d, Count = weekdayMap.GetValueOrDefault(d, 0) })
            .ToList();

        // ── Profil (extra) kesimlari ─────────────────────────────────
        var profiledUsers = await context.UserExtras.CountAsync();

        var genderRows = await context.UserExtras
            .Where(x => x.Gender == EnumGender.Male || x.Gender == EnumGender.Female)
            .GroupBy(x => x.Gender)
            .Select(g => new GenderCountDto { Gender = g.Key, Count = g.Count() })
            .ToListAsync();

        // Yosh guruhlari — tug'ilgan yil orqali (default/bo'sh sanalarni chetlab).
        var birthYears = await context.UserExtras
            .Where(x => x.BirthDate.Year > 1920)
            .Select(x => x.BirthDate.Year)
            .ToListAsync();
        var ageGroups = BuildAgeGroups(birthYears, now.Year);

        var purposeRows = await context.UserExtras
            .GroupBy(x => x.Purpose)
            .Select(g => new { Key = g.Key, Count = g.Count() })
            .ToListAsync();
        var purposeBreakdown = MapEnumCounts<EnumPurpose>(purposeRows.Select(r => ((int)r.Key, r.Count)));

        var activityRows = await context.UserExtras
            .GroupBy(x => x.ActivityLevel)
            .Select(g => new { Key = g.Key, Count = g.Count() })
            .ToListAsync();
        var activityBreakdown = MapEnumCounts<EnumActivityLevel>(activityRows.Select(r => ((int)r.Key, r.Count)));

        var languageRows = await context.UserExtras
            .GroupBy(x => x.Language)
            .Select(g => new { Key = g.Key, Count = g.Count() })
            .ToListAsync();
        var languageBreakdown = MapEnumCounts<EnumLanguage>(languageRows.Select(r => ((int)r.Key, r.Count)));

        return new GetAudienceAnalyticsDto
        {
            TotalUsers = totalUsers,
            ProfiledUsers = profiledUsers,
            HourlyRegistrations = hourly,
            WeekdayRegistrations = weekdays,
            PeakHour = peakHour,
            GenderBreakdown = genderRows,
            AgeGroups = ageGroups,
            PurposeBreakdown = purposeBreakdown,
            ActivityLevelBreakdown = activityBreakdown,
            LanguageBreakdown = languageBreakdown,
        };
    }

    private static List<EnumCountDto> MapEnumCounts<TEnum>(IEnumerable<(int Key, int Count)> rows)
        where TEnum : struct, Enum =>
        rows
            .Select(r => new EnumCountDto
            {
                Value = r.Key,
                Name = Enum.IsDefined(typeof(TEnum), r.Key)
                    ? Enum.GetName(typeof(TEnum), r.Key)!
                    : "Unknown",
                Count = r.Count,
            })
            .OrderBy(x => x.Value)
            .ToList();

    private static List<AgeGroupCountDto> BuildAgeGroups(IReadOnlyCollection<int> birthYears, int currentYear)
    {
        string[] labels = ["<18", "18-24", "25-34", "35-44", "45-54", "55+"];
        var counts = labels.ToDictionary(l => l, _ => 0);
        foreach (var year in birthYears)
        {
            var age = currentYear - year;
            var label = age switch
            {
                < 18 => "<18",
                <= 24 => "18-24",
                <= 34 => "25-34",
                <= 44 => "35-44",
                <= 54 => "45-54",
                _ => "55+",
            };
            counts[label]++;
        }

        return labels.Select(l => new AgeGroupCountDto { Group = l, Count = counts[l] }).ToList();
    }

    public async Task<GetUserDetailDto?> GetUserDetail(long userId)
    {
        var detail = await context.Users
            .Where(x => x.Id == userId)
            .Select(x => new GetUserDetailDto
            {
                Id = x.Id,
                Name = x.Name,
                Email = x.Email,
                Phone = x.Phone,
                Roles = x.Roles,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt,
                Subscription = x.Subscription != null
                    ? new SubscriptionDto
                    {
                        Id = x.Subscription.Id,
                        StartsAt = x.Subscription.StartsAt,
                        EndsAt = x.Subscription.EndsAt,
                        Plan = x.Subscription.SubscriptionPlan,
                        IsActive = x.Subscription.IsActive,
                    }
                    : null,
                Extra = x.Extra != null
                    ? new UserDetailExtraDto
                    {
                        Weight = x.Extra.Weight,
                        EntryWeight = x.Extra.EntryWeight,
                        Height = x.Extra.Height,
                        Bmi = Math.Round(x.Extra.Bmi, 1),
                        Gender = x.Extra.Gender,
                        BirthDate = x.Extra.BirthDate,
                        Purpose = x.Extra.Purpose,
                        PhysicalActivity = x.Extra.PhysicalActivity,
                        ActivityLevel = x.Extra.ActivityLevel,
                        Language = x.Extra.Language,
                        Photo = x.Extra.Photo,
                    }
                    : null,
            })
            .FirstOrDefaultAsync();

        if (detail is null) return null;

        if (detail.Extra is not null)
            detail.Extra.Age = Math.Max(DateTime.Now.Year - detail.Extra.BirthDate.Year, 0);

        detail.SignInCount = await context.SignLogs.CountAsync(x => x.UserId == userId);
        detail.LastSignInAt = await context.SignLogs
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.SignAt)
            .Select(x => (DateTime?)x.SignAt)
            .FirstOrDefaultAsync();

        detail.Norms = await context.UserNorms
            .Where(x => x.UserId == userId)
            .Select(x => new UserNormValueDto { Metric = x.Metric, Value = Math.Round(x.Value, 1) })
            .ToListAsync();

        return detail;
    }

    private static List<DailyCountDto> FillDailySeries(
        IReadOnlyDictionary<DateTime, int> counts, DateTime from, DateTime to)
    {
        var series = new List<DailyCountDto>();
        for (var day = from; day <= to; day = day.AddDays(1))
            series.Add(new DailyCountDto { Date = day, Count = counts.GetValueOrDefault(day, 0) });
        return series;
    }

    // O'sish foizi. Oldingi davrda ma'lumot bo'lmasa (0), -100% ko'rsatish noto'g'ri:
    // hozir ham 0 bo'lsa o'zgarish yo'q (0%), aks holda to'liq o'sish (+100%).
    private static double GrowthPercent(double current, double previous)
    {
        if (previous <= 0) return current > 0 ? 100 : 0;
        return Math.Round((current / previous - 1) * 100, 2);
    }

    public async Task<Wrapper> GetSubscriptionOrders(DataQueryRequest query)
    {
        return await context
            .SubscriptionOrders
            // Eng yangi sotuvlar birinchi kelsin — mijoz sort bermasa ham so'nggi
            // kunlardagi to'lovlar ro'yxatning boshida ko'rinadi.
            .OrderByDescending(x => x.Order.CreatedAt)
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