using BRB.Core.EF.Attributes;
using Core.Brokers.DbContext;
using Core.Entities.Coins.Enum;
using Core.Enums;
using Core.Services.Reports.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Core.Services.Reports;

/// <summary>
/// Haftalik hisobot: <c>daily_menus</c> (kkal/makros), <c>user_dailies</c> (qadam, suv)
/// va qadam coinlaridan yig'iladi. Alohida saqlanmaydi — har so'rovda hisoblanadi.
/// </summary>
[Injectable]
public class WeeklyReportService(AppDbContext dbContext)
{
    /// <summary><c>FoodService.DefaultFoodWeightMetric</c> bilan bir xil — metrikasi yo'q taom porsiyasi (g).</summary>
    private const int DefaultFoodWeightMetric = 400;

    private const double NormLowerRatio = 0.75;
    private const double NormUpperRatio = 1.10;
    private const int StreakLookbackDays = 365;

    private const int PerfectWeekDays = 5;
    private const double StepMasterSteps = 70_000;
    private const double ProteinProRatio = 0.9;
    private const int HydratedDays = 5;

    private record MenuRow(DateTime Date, EnumMenu Menu, long FoodId, double Kcal, double Protein, double Fat, double Carb);

    /// <summary>
    /// <paramref name="weekStart"/> ichidagi haftaning dushanbasidan boshlab 7 kun.
    /// Berilmasa — o'tgan hafta.
    /// </summary>
    public async Task<WeeklyReportDto> GetWeekly(long userId, DateTime? weekStart)
    {
        var start = StartOfWeek(weekStart?.Date ?? DateTime.Now.Date.AddDays(-7));
        var end = start.AddDays(7);
        var prevStart = start.AddDays(-7);

        var name = await dbContext.Users
            .Where(u => u.Id == userId)
            .Select(u => u.Name)
            .FirstOrDefaultAsync() ?? string.Empty;

        // UserNorms faqat user_norms jadvalini o'qiydi (user_dailies bilan UNION qilmaydi).
        var normRows = await dbContext.UserNorms
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .Select(x => new { x.Metric, x.Value })
            .ToListAsync();
        var normMap = normRows.GroupBy(x => x.Metric).ToDictionary(g => g.Key, g => g.First().Value);
        var norms = new WeeklyNormsDto
        {
            Kcal = normMap.GetValueOrDefault(EnumMetrics.Kcal),
            Protein = normMap.GetValueOrDefault(EnumMetrics.Protein),
            Fat = normMap.GetValueOrDefault(EnumMetrics.Fat),
            Carb = normMap.GetValueOrDefault(EnumMetrics.Carb),
            Water = normMap.GetValueOrDefault(EnumMetrics.Water),
            Step = normMap.GetValueOrDefault(EnumMetrics.Step)
        };

        var menus = await LoadMenus(userId, prevStart, end);
        var weekMenus = menus.Where(m => m.Date >= start).ToList();
        var prevMenus = menus.Where(m => m.Date < start).ToList();

        var dailies = await dbContext.UserDailies
            .AsNoTracking()
            .Where(d => d.UserId == userId && d.Date >= prevStart && d.Date < end &&
                        (d.Metric == EnumMetrics.Step || d.Metric == EnumMetrics.Water))
            .Select(d => new { d.Date, d.Metric, d.Value })
            .ToListAsync();

        double DailySum(DateTime date, EnumMetrics metric) =>
            dailies.Where(d => d.Date.Date == date && d.Metric == metric).Sum(d => d.Value);

        var days = Enumerable.Range(0, 7).Select(i =>
        {
            var date = start.AddDays(i);
            var dayMenus = weekMenus.Where(m => m.Date == date).ToList();
            var kcal = Math.Round(dayMenus.Sum(m => m.Kcal));

            return new WeeklyDayDto
            {
                Date = date,
                Kcal = kcal,
                Protein = Math.Round(dayMenus.Sum(m => m.Protein)),
                Fat = Math.Round(dayMenus.Sum(m => m.Fat)),
                Carb = Math.Round(dayMenus.Sum(m => m.Carb)),
                Water = DailySum(date, EnumMetrics.Water),
                Steps = DailySum(date, EnumMetrics.Step),
                MealCount = dayMenus.Count,
                InNorm = dayMenus.Count > 0 && norms.Kcal > 0 &&
                         kcal >= norms.Kcal * NormLowerRatio && kcal <= norms.Kcal * NormUpperRatio
            };
        }).ToList();

        var logged = days.Where(d => d.MealCount > 0).ToList();
        var totals = new WeeklyTotalsDto
        {
            Kcal = days.Sum(d => d.Kcal),
            Protein = days.Sum(d => d.Protein),
            Fat = days.Sum(d => d.Fat),
            Carb = days.Sum(d => d.Carb),
            Water = days.Sum(d => d.Water),
            Steps = days.Sum(d => d.Steps),
            MealCount = days.Sum(d => d.MealCount)
        };

        // Ovqat o'rtachasi — yozilgan kunlar bo'yicha (yozilmagan kun 0 kkal emas); qadam/suv — 7 kun.
        var loggedCount = Math.Max(logged.Count, 1);
        var averages = new WeeklyTotalsDto
        {
            Kcal = Math.Round(totals.Kcal / loggedCount),
            Protein = Math.Round(totals.Protein / loggedCount),
            Fat = Math.Round(totals.Fat / loggedCount),
            Carb = Math.Round(totals.Carb / loggedCount),
            Water = Math.Round(totals.Water / 7, 1),
            Steps = Math.Round(totals.Steps / 7),
            MealCount = (int)Math.Round((double)totals.MealCount / loggedCount)
        };

        var topFood = await GetTopFood(weekMenus);

        var prevLoggedDays = prevMenus.Select(m => m.Date).Distinct().Count();
        var prevKcalAvg = prevLoggedDays > 0 ? prevMenus.Sum(m => m.Kcal) / prevLoggedDays : 0;
        var prevSteps = dailies.Where(d => d.Date < start && d.Metric == EnumMetrics.Step).Sum(d => d.Value);

        var daysInNorm = days.Count(d => d.InNorm);

        return new WeeklyReportDto
        {
            Name = name,
            WeekStart = start,
            WeekEnd = end.AddDays(-1),
            LoggedDays = logged.Count,
            DaysInNorm = daysInNorm,
            Norms = norms,
            Days = days,
            Totals = totals,
            Averages = averages,
            KcalByMenu = Enum.GetValues<EnumMenu>()
                .ToDictionary(menu => menu, menu => Math.Round(weekMenus.Where(m => m.Menu == menu).Sum(m => m.Kcal))),
            TopFood = topFood,
            HeaviestDay = logged.Count > 0 ? logged.MaxBy(d => d.Kcal)!.Date : null,
            MostActiveDay = totals.Steps > 0 ? days.MaxBy(d => d.Steps)!.Date : null,
            CoinsEarned = await GetStepCoins(userId, start, end),
            Streak = await GetStreak(userId, end),
            KcalAvgChangePercent = logged.Count > 0 ? ChangePercent(prevKcalAvg, averages.Kcal) : null,
            StepsChangePercent = ChangePercent(prevSteps, totals.Steps),
            Badges = GetBadges(days, norms, daysInNorm, logged.Count, totals, averages)
        };
    }

    private async Task<List<MenuRow>> LoadMenus(long userId, DateTime from, DateTime to)
    {
        var rows = await dbContext.DailyMenus
            .AsNoTracking()
            .Where(x => x.UserId == userId && x.Date >= from && x.Date < to)
            .Select(x => new
            {
                x.Date,
                x.Menu,
                x.FoodId,
                x.Weight,
                Metrics = x.Food.Metrics.Select(m => new { m.Metric, m.Value }).ToList()
            })
            .ToListAsync();

        // Hisob FoodService.Summary bilan bir xil: metrika qiymati / taom og'irligi * yeyilgan og'irlik.
        return rows.Select(r =>
        {
            var baseWeight = r.Metrics.FirstOrDefault(m => m.Metric == EnumMetrics.Weight)?.Value ??
                             DefaultFoodWeightMetric;
            if (baseWeight <= 0) baseWeight = DefaultFoodWeightMetric;

            double Per(EnumMetrics metric) =>
                (r.Metrics.FirstOrDefault(m => m.Metric == metric)?.Value ?? 0) / baseWeight * r.Weight;

            return new MenuRow(r.Date.Date, r.Menu, r.FoodId,
                Per(EnumMetrics.Kcal), Per(EnumMetrics.Protein), Per(EnumMetrics.Fat), Per(EnumMetrics.Carb));
        }).ToList();
    }

    private async Task<WeeklyTopFoodDto?> GetTopFood(List<MenuRow> weekMenus)
    {
        var top = weekMenus
            .GroupBy(m => m.FoodId)
            .Select(g => new { FoodId = g.Key, Count = g.Count(), Kcal = g.Sum(m => m.Kcal) })
            .OrderByDescending(x => x.Count)
            .ThenByDescending(x => x.Kcal)
            .FirstOrDefault();

        if (top is null) return null;

        return await dbContext.Foods
            .AsNoTracking()
            .Where(f => f.Id == top.FoodId)
            .Select(f => new WeeklyTopFoodDto
            {
                Id = f.Id,
                Name = f.Name,
                CoverUrl = f.CoverUrl,
                Count = top.Count
            })
            .FirstOrDefaultAsync();
    }

    /// <summary>Qadam coinlari kuniga bitta yozuv, <c>RefId</c> = yyyyMMdd.</summary>
    private Task<long> GetStepCoins(long userId, DateTime start, DateTime end)
    {
        var fromRef = long.Parse(start.ToString("yyyyMMdd"));
        var toRef = long.Parse(end.AddDays(-1).ToString("yyyyMMdd"));

        return dbContext.CoinTransactions
            .Where(t => t.UserId == userId && t.Type == EnumCoinTxType.Steps &&
                        t.RefId >= fromRef && t.RefId <= toRef)
            .SumAsync(t => t.Amount);
    }

    /// <summary>Hafta oxiridan (<paramref name="end"/> dan oldingi kun) orqaga ketma-ket ovqat yozilgan kunlar.</summary>
    private async Task<int> GetStreak(long userId, DateTime end)
    {
        var dates = await dbContext.DailyMenus
            .AsNoTracking()
            .Where(x => x.UserId == userId && x.Date < end && x.Date >= end.AddDays(-StreakLookbackDays))
            .Select(x => x.Date.Date)
            .Distinct()
            .ToListAsync();

        var set = dates.ToHashSet();
        var streak = 0;
        for (var day = end.AddDays(-1); set.Contains(day); day = day.AddDays(-1))
            streak++;

        return streak;
    }

    private static List<string> GetBadges(List<WeeklyDayDto> days, WeeklyNormsDto norms, int daysInNorm,
        int loggedDays, WeeklyTotalsDto totals, WeeklyTotalsDto averages)
    {
        var badges = new List<string>();

        if (daysInNorm >= PerfectWeekDays) badges.Add("perfect_week");
        if (loggedDays == 7) badges.Add("consistent");
        if (totals.Steps >= StepMasterSteps) badges.Add("step_master");
        if (loggedDays > 0 && norms.Protein > 0 && averages.Protein >= norms.Protein * ProteinProRatio)
            badges.Add("protein_pro");
        if (norms.Water > 0 && days.Count(d => d.Water >= norms.Water) >= HydratedDays) badges.Add("hydrated");

        return badges;
    }

    private static double? ChangePercent(double previous, double current) =>
        previous > 0 ? Math.Round((current - previous) / previous * 100, 1) : null;

    private static DateTime StartOfWeek(DateTime date)
    {
        var diff = ((int)date.DayOfWeek + 6) % 7; // Dushanba = 0
        return date.Date.AddDays(-diff);
    }
}
