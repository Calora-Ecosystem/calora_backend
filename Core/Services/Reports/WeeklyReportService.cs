using BRB.Core.EF.Attributes;
using Core.Brokers.DbContext;
using Core.Entities.Coins.Enum;
using Core.Entities.Course.Enum;
using Core.Enums;
using Core.Services.Reports.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Core.Services.Reports;

/// <summary>
/// Haftalik hisobot: <c>daily_menus</c> (kkal/makros), <c>user_dailies</c> (qadam, suv),
/// coinlar, profil (vazn), kurs progressi, takliflar va qadam guruhlaridan yig'iladi. Alohida saqlanmaydi — har so'rovda hisoblanadi.
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
    private const int StepGoalDays = 5;

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
            Step = normMap.GetValueOrDefault(EnumMetrics.Step),
            Weight = normMap.GetValueOrDefault(EnumMetrics.Weight)
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
        var stepDaysInNorm = norms.Step > 0 ? days.Count(d => d.Steps >= norms.Step) : 0;
        var waterDaysInNorm = norms.Water > 0 ? days.Count(d => d.Water >= norms.Water) : 0;
        var coins = await GetCoins(userId, start, end);

        return new WeeklyReportDto
        {
            Name = name,
            WeekStart = start,
            WeekEnd = end.AddDays(-1),
            LoggedDays = logged.Count,
            DaysInNorm = daysInNorm,
            ActiveDays = days.Count(d => d.MealCount > 0 || d.Steps > 0 || d.Water > 0),
            StepDaysInNorm = stepDaysInNorm,
            WaterDaysInNorm = waterDaysInNorm,
            Norms = norms,
            Days = days,
            Totals = totals,
            Averages = averages,
            KcalByMenu = Enum.GetValues<EnumMenu>()
                .ToDictionary(menu => menu, menu => Math.Round(weekMenus.Where(m => m.Menu == menu).Sum(m => m.Kcal))),
            TopFood = topFood,
            HeaviestDay = logged.Count > 0 ? logged.MaxBy(d => d.Kcal)!.Date : null,
            MostActiveDay = totals.Steps > 0 ? days.MaxBy(d => d.Steps)!.Date : null,
            CoinsEarned = coins.Steps,
            Streak = await GetStreak(userId, end),
            KcalAvgChangePercent = logged.Count > 0 ? ChangePercent(prevKcalAvg, averages.Kcal) : null,
            StepsChangePercent = ChangePercent(prevSteps, totals.Steps),
            Body = await GetBody(userId, norms),
            Coins = coins,
            Course = await GetCourse(userId, start, end),
            FriendsInvited = await dbContext.Referrals
                .CountAsync(r => r.ReferrerId == userId && r.CreatedAt >= start && r.CreatedAt < end),
            StepGroups = await dbContext.StepGroupMembers.CountAsync(m => m.UserId == userId),
            Badges = GetBadges(days, norms, daysInNorm, stepDaysInNorm, logged.Count, totals, averages)
        };
    }

    private async Task<WeeklyBodyDto> GetBody(long userId, WeeklyNormsDto norms)
    {
        var body = await dbContext.UserExtras
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .Select(x => new WeeklyBodyDto
            {
                Weight = x.Weight,
                EntryWeight = x.EntryWeight,
                Height = x.Height,
                Bmi = x.Bmi,
                Purpose = x.Purpose
            })
            .FirstOrDefaultAsync() ?? new WeeklyBodyDto();

        body.TargetWeight = norms.Weight;
        body.Bmi = Math.Round(body.Bmi, 1);
        return body;
    }

    /// <summary>
    /// Hafta ichidagi coin tranzaksiyalari. Qadam coinlari <c>RefId</c> (yyyyMMdd) bo'yicha,
    /// qolganlari <c>CreatedAt</c> bo'yicha olinadi — qadam yozuvi kun davomida yangilanadi.
    /// </summary>
    private async Task<WeeklyCoinsDto> GetCoins(long userId, DateTime start, DateTime end)
    {
        var fromRef = long.Parse(start.ToString("yyyyMMdd"));
        var toRef = long.Parse(end.AddDays(-1).ToString("yyyyMMdd"));

        var steps = await dbContext.CoinTransactions
            .Where(t => t.UserId == userId && t.Type == EnumCoinTxType.Steps &&
                        t.RefId >= fromRef && t.RefId <= toRef)
            .SumAsync(t => t.Amount);

        var others = await dbContext.CoinTransactions
            .AsNoTracking()
            .Where(t => t.UserId == userId && t.Type != EnumCoinTxType.Steps &&
                        t.CreatedAt >= start && t.CreatedAt < end)
            .Select(t => new { t.Type, t.Amount })
            .ToListAsync();

        var balance = await dbContext.CoinWallets
            .Where(w => w.UserId == userId)
            .Select(w => (long?)w.Balance)
            .FirstOrDefaultAsync() ?? 0;

        return new WeeklyCoinsDto
        {
            Steps = steps,
            Referral = others.Where(t => t.Type == EnumCoinTxType.Referral && t.Amount > 0).Sum(t => t.Amount),
            Earned = steps + others.Where(t => t.Amount > 0).Sum(t => t.Amount),
            Spent = -others.Where(t => t.Amount < 0).Sum(t => t.Amount),
            Balance = balance
        };
    }

    /// <summary>Hafta ichida tugatilgan (yoki qayta tugatilgan) darslar, mashqlar va workoutlar.</summary>
    private async Task<WeeklyCourseDto> GetCourse(long userId, DateTime start, DateTime end)
    {
        var counts = await dbContext.CourseItemStates
            .AsNoTracking()
            .Where(x => x.UserId == userId && x.UpdatedAt >= start && x.UpdatedAt < end)
            .GroupBy(x => x.Type)
            .Select(g => new { Type = g.Key, Count = g.Count() })
            .ToListAsync();

        int Count(EnumEntityType type) => counts.FirstOrDefault(c => c.Type == type)?.Count ?? 0;

        return new WeeklyCourseDto
        {
            Lessons = Count(EnumEntityType.Lesson),
            Exercises = Count(EnumEntityType.Exercise),
            Workouts = Count(EnumEntityType.Workout)
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
        int stepDaysInNorm, int loggedDays, WeeklyTotalsDto totals, WeeklyTotalsDto averages)
    {
        var badges = new List<string>();

        if (daysInNorm >= PerfectWeekDays) badges.Add("perfect_week");
        if (loggedDays == 7) badges.Add("consistent");
        if (totals.Steps >= StepMasterSteps) badges.Add("step_master");
        if (stepDaysInNorm >= StepGoalDays) badges.Add("step_goal");
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
