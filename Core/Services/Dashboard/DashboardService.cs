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

    /// <summary>Hozircha jurnalda qayd etilgan barcha Source qiymatlari — filtr uchun (masalan frontend dropdown).</summary>
    public async Task<List<string>> GetEventLogSources()
    {
        return await context.EventLogs
            .Select(x => x.Source)
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync();
    }

    public async Task<GetEventLogSummaryDto> GetEventLogSummary(string source, DateTime? from, DateTime? to)
    {
        var toEnd = (to ?? DateTime.Now).Date.AddDays(1);
        var fromStart = (from ?? toEnd.AddDays(-30)).Date;
        if (toEnd <= fromStart) toEnd = fromStart.AddDays(1);
        var lastDay = toEnd.AddDays(-1);

        var query = context.EventLogs
            .Where(x => x.Source == source && x.CreatedAt >= fromStart && x.CreatedAt < toEnd);

        var totalCount = await query.CountAsync();
        var successCount = await query.CountAsync(x => x.Status == EnumEventStatus.Success);
        var warningCount = await query.CountAsync(x => x.Status == EnumEventStatus.Warning);
        var errorCount = await query.CountAsync(x => x.Status == EnumEventStatus.Error);

        var avgDurationMs = await query.Where(x => x.DurationMs != null).AverageAsync(x => (double?)x.DurationMs);
        var maxDurationMs = await query.Where(x => x.DurationMs != null).MaxAsync(x => (long?)x.DurationMs);

        var outcomeBreakdown = await query
            .GroupBy(x => new { x.Outcome, x.Status })
            .Select(g => new EventOutcomeCountDto { Outcome = g.Key.Outcome, Status = g.Key.Status, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ToListAsync();

        var topErrorTypes = await query
            .Where(x => x.Status == EnumEventStatus.Error && x.ErrorType != null)
            .GroupBy(x => x.ErrorType!)
            .Select(g => new { ErrorType = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(10)
            .ToListAsync();

        var topErrors = new List<EventErrorCountDto>();
        foreach (var e in topErrorTypes)
        {
            var sample = await query
                .Where(x => x.ErrorType == e.ErrorType)
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => x.ErrorMessage)
                .FirstOrDefaultAsync();
            topErrors.Add(new EventErrorCountDto { ErrorType = e.ErrorType, Count = e.Count, SampleMessage = sample });
        }

        // Kunlik trendni SQL'da emas, xotirada hisoblaymiz — DateTime.Date bo'yicha
        // guruhlash + Status shartli sanoqlari provayderga bog'liq/ishonchsiz bo'lishi mumkin.
        var rows = await query.Select(x => new { x.CreatedAt, x.Status }).ToListAsync();
        var dailyTrend = new List<DailyEventCountDto>();
        for (var day = fromStart; day <= lastDay; day = day.AddDays(1))
        {
            var dayRows = rows.Where(r => r.CreatedAt.Date == day).ToList();
            dailyTrend.Add(new DailyEventCountDto
            {
                Date = day,
                Total = dayRows.Count,
                Success = dayRows.Count(r => r.Status == EnumEventStatus.Success),
                Warning = dayRows.Count(r => r.Status == EnumEventStatus.Warning),
                Error = dayRows.Count(r => r.Status == EnumEventStatus.Error),
            });
        }

        return new GetEventLogSummaryDto
        {
            Source = source,
            From = fromStart,
            To = lastDay,
            TotalCount = totalCount,
            SuccessCount = successCount,
            WarningCount = warningCount,
            ErrorCount = errorCount,
            ErrorRatePercent = totalCount > 0 ? Math.Round(errorCount * 100d / totalCount, 2) : 0,
            AvgDurationMs = avgDurationMs.HasValue ? Math.Round(avgDurationMs.Value, 1) : null,
            MaxDurationMs = maxDurationMs,
            OutcomeBreakdown = outcomeBreakdown,
            TopErrors = topErrors,
            DailyTrend = dailyTrend,
        };
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

    #region AI Analytics (food_recognition statistics)

    // Gemini 2.5 Flash pricing:
    // Prompt: $0.30 per 1,000,000 tokens ($0.00000030/token)
    // Candidate: $2.50 per 1,000,000 tokens ($0.00000250/token)
    private const double PromptTokenPricePerMillion = 0.30;
    private const double CandidateTokenPricePerMillion = 2.50;

    public async Task<GetAiStatisticsDto> GetAiStatistics(int? days = 28, DateTime? from = null, DateTime? to = null)
    {
        var effectiveDays = days is > 0 ? days.Value : 28;
        var toEnd = (to ?? DateTime.Now).Date.AddDays(1);
        var fromStart = from?.Date ?? toEnd.AddDays(-effectiveDays).Date;
        if (toEnd <= fromStart) toEnd = fromStart.AddDays(1);
        var totalDays = Math.Max((int)(toEnd.Date - fromStart.Date).TotalDays, 1);
        var lastDay = toEnd.AddDays(-1);

        const string source = "ai.food_recognition";
        var query = context.EventLogs
            .AsNoTracking()
            .Where(x => x.Source == source && x.CreatedAt >= fromStart && x.CreatedAt < toEnd);

        var rows = await query
            .Select(x => new
            {
                x.Id,
                x.UserId,
                x.CreatedAt,
                x.Status,
                x.DurationMs,
                x.Metadata
            })
            .ToListAsync();

        var totalRequests = rows.Count;
        var successRequests = rows.Count(r => r.Status == EnumEventStatus.Success);
        var errorRequests = rows.Count(r => r.Status == EnumEventStatus.Error);

        var userGroups = rows
            .Where(r => r.UserId.HasValue)
            .GroupBy(r => r.UserId!.Value)
            .ToList();

        var totalAiUsers = userGroups.Count;

        var userRequestCounts = userGroups.Select(g => g.Count()).ToList();
        var minRequestsPerUser = userRequestCounts.Count > 0 ? userRequestCounts.Min() : 0;
        var maxRequestsPerUser = userRequestCounts.Count > 0 ? userRequestCounts.Max() : 0;

        var avgRequestsPerUser = totalAiUsers > 0
            ? Math.Round((double)totalRequests / totalAiUsers, 2)
            : 0;

        var avgRequestsPerDay = totalDays > 0
            ? Math.Round((double)totalRequests / totalDays, 2)
            : 0;

        var dayGroups = rows
            .GroupBy(r => r.CreatedAt.Date)
            .ToDictionary(g => g.Key, g => g.Count());

        var minRequestsPerDay = 0;
        var maxRequestsPerDay = 0;
        if (dayGroups.Count > 0)
        {
            minRequestsPerDay = dayGroups.Count < totalDays ? 0 : dayGroups.Values.Min();
            maxRequestsPerDay = dayGroups.Values.Max();
        }

        var durations = rows.Where(r => r.DurationMs.HasValue).Select(r => (double)r.DurationMs!.Value).ToList();
        double? avgDurationMs = durations.Count > 0 ? Math.Round(durations.Average(), 1) : null;

        long totalPromptTokens = 0;
        long totalCandidateTokens = 0;

        foreach (var r in rows)
        {
            if (r.Metadata != null)
            {
                if (r.Metadata.TryGetValue("promptTokens", out var pt) && long.TryParse(pt, out var pVal))
                    totalPromptTokens += pVal;
                if (r.Metadata.TryGetValue("candidateTokens", out var ct) && long.TryParse(ct, out var cVal))
                    totalCandidateTokens += cVal;
            }
        }

        var totalTokens = totalPromptTokens + totalCandidateTokens;
        var totalCostUsd = Math.Round(
            (totalPromptTokens * PromptTokenPricePerMillion + totalCandidateTokens * CandidateTokenPricePerMillion) / 1_000_000.0,
            4);

        var costPerUserUsd = totalAiUsers > 0
            ? Math.Round(totalCostUsd / totalAiUsers, 6)
            : 0;

        var costPerRequestUsd = totalRequests > 0
            ? Math.Round(totalCostUsd / totalRequests, 6)
            : 0;

        var aiUserIds = userGroups.Select(g => g.Key).ToHashSet();

        var premiumUserIds = await context.Subscriptions
            .AsNoTracking()
            .Where(s => s.SubscriptionPlan == EnumSPlans.Premium)
            .Select(s => s.UserId)
            .Distinct()
            .ToListAsync();

        var totalPremiumUsers = premiumUserIds.Count;
        var activeAiPremiumUsers = premiumUserIds.Count(id => aiUserIds.Contains(id));
        var aiAdoptionRatePercent = totalPremiumUsers > 0
            ? Math.Round((double)activeAiPremiumUsers * 100.0 / totalPremiumUsers, 2)
            : 0;

        var dailyTrend = new List<DailyAiStatDto>();
        for (var day = fromStart; day <= lastDay; day = day.AddDays(1))
        {
            var dayRows = rows.Where(r => r.CreatedAt.Date == day).ToList();
            long dayPrompt = 0;
            long dayCandidate = 0;

            foreach (var r in dayRows)
            {
                if (r.Metadata != null)
                {
                    if (r.Metadata.TryGetValue("promptTokens", out var pt) && long.TryParse(pt, out var pVal))
                        dayPrompt += pVal;
                    if (r.Metadata.TryGetValue("candidateTokens", out var ct) && long.TryParse(ct, out var cVal))
                        dayCandidate += cVal;
                }
            }

            var dayCost = Math.Round(
                (dayPrompt * PromptTokenPricePerMillion + dayCandidate * CandidateTokenPricePerMillion) / 1_000_000.0,
                4);

            dailyTrend.Add(new DailyAiStatDto
            {
                Date = day,
                Requests = dayRows.Count,
                Users = dayRows.Where(r => r.UserId.HasValue).Select(r => r.UserId!.Value).Distinct().Count(),
                CostUsd = dayCost,
                PromptTokens = dayPrompt,
                CandidateTokens = dayCandidate
            });
        }

        var topUsers = userGroups
            .Select(g =>
            {
                long uPrompt = 0;
                long uCandidate = 0;
                foreach (var r in g)
                {
                    if (r.Metadata != null)
                    {
                        if (r.Metadata.TryGetValue("promptTokens", out var pt) && long.TryParse(pt, out var pVal))
                            uPrompt += pVal;
                        if (r.Metadata.TryGetValue("candidateTokens", out var ct) && long.TryParse(ct, out var cVal))
                            uCandidate += cVal;
                    }
                }

                var uCost = Math.Round(
                    (uPrompt * PromptTokenPricePerMillion + uCandidate * CandidateTokenPricePerMillion) / 1_000_000.0,
                    4);

                return new TopAiUserDto
                {
                    UserId = g.Key,
                    RequestCount = g.Count(),
                    CostUsd = uCost
                };
            })
            .OrderByDescending(u => u.RequestCount)
            .Take(10)
            .ToList();

        return new GetAiStatisticsDto
        {
            From = fromStart,
            To = lastDay,
            Days = totalDays,
            TotalAiUsers = totalAiUsers,
            TotalRequests = totalRequests,
            SuccessRequests = successRequests,
            ErrorRequests = errorRequests,
            AvgRequestsPerUser = avgRequestsPerUser,
            AvgRequestsPerDay = avgRequestsPerDay,
            MinRequestsPerUser = minRequestsPerUser,
            MaxRequestsPerUser = maxRequestsPerUser,
            MinRequestsPerDay = minRequestsPerDay,
            MaxRequestsPerDay = maxRequestsPerDay,
            AvgDurationMs = avgDurationMs,
            TotalPromptTokens = totalPromptTokens,
            TotalCandidateTokens = totalCandidateTokens,
            TotalTokens = totalTokens,
            TotalCostUsd = totalCostUsd,
            CostPerUserUsd = costPerUserUsd,
            CostPerRequestUsd = costPerRequestUsd,
            TotalPremiumUsers = totalPremiumUsers,
            ActiveAiPremiumUsers = activeAiPremiumUsers,
            AiAdoptionRatePercent = aiAdoptionRatePercent,
            DailyTrend = dailyTrend,
            TopUsers = topUsers
        };
    }

    #endregion
}