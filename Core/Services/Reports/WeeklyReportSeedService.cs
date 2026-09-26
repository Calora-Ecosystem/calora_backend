using BRB.Core.EF.Attributes;
using Core.Brokers.DbContext;
using Core.Entities.Auth;
using Core.Entities.FoodEntites;
using Core.Enums;
using Microsoft.EntityFrameworkCore;

namespace Core.Services.Reports;

/// <summary>
/// VAQTINCHALIK (faqat test uchun): haftalik hisobotni ko'rish uchun bitta userga
/// 2 haftalik ovqat, qadam va suv datasini qo'shadi / o'chiradi.
/// Test tugagach <c>DevSeedController</c> bilan birga o'chiriladi.
/// </summary>
[Injectable]
public class WeeklyReportSeedService(AppDbContext dbContext)
{
    private const int DefaultFoodWeightMetric = 400;
    private const int FoodPoolSize = 14;

    // O'tgan hafta: 7 kundan 5 tasi normaning 75–110% oralig'ida.
    private static readonly double[] LastKcal = [0.92, 1.05, 0.86, 1.20, 0.95, 1.32, 0.98];
    private static readonly double[] PrevKcal = [1.15, 1.25, 1.05, 1.30, 1.10, 1.40, 1.20];
    private static readonly int[] LastSteps = [8213, 10427, 6108, 12846, 9032, 4317, 11020];
    private static readonly int[] PrevSteps = [6120, 7304, 5011, 8233, 6540, 3902, 7015];
    private static readonly double[] LastWater = [2, 2.5, 1.75, 2, 2.25, 1.5, 2];
    private static readonly double[] PrevWater = [1.5, 1.75, 1.25, 2, 1.5, 1, 1.5];

    /// <summary>Nonushta, tushlik, kechki ovqat, gazak ulushi (FoodService.Summary normalari bilan bir xil).</summary>
    private static readonly (EnumMenu Menu, double Share)[] Meals =
    [
        (EnumMenu.Breakfast, 0.25), (EnumMenu.Lunch, 0.35), (EnumMenu.Dinner, 0.30), (EnumMenu.Snack, 0.10)
    ];

    public record SeedResultDto(long UserId, DateTime From, DateTime To, int Meals, int Dailies);

    /// <summary>
    /// <paramref name="weekStart"/> haftasi va undan oldingi haftani to'ldiradi (default — o'tgan hafta).
    /// Userda shu davrda ovqat bo'lsa qayta qo'shmaydi; mavjud qadam/suv qiymatlari saqlanadi.
    /// </summary>
    public async Task<SeedResultDto> Seed(string phone, DateTime? weekStart)
    {
        var userId = await FindUser(phone);
        var (from, to) = Period(weekStart);

        if (await dbContext.DailyMenus.AnyAsync(x => x.UserId == userId && x.Date >= from && x.Date < to))
            throw new Exceptions.BadRequestException("seed_user_has_meals");

        var kcalNorm = await dbContext.UserNorms
            .Where(x => x.UserId == userId && x.Metric == EnumMetrics.Kcal)
            .Select(x => (double?)x.Value)
            .FirstOrDefaultAsync();
        var norm = kcalNorm is > 0 ? kcalNorm.Value : 2000;

        // Umumiy katalogdan kkal qiymati bor taomlar; kkal/gramm API'dagi formula bilan.
        var pool = await dbContext.Foods
            .AsNoTracking()
            .Where(f => f.UserId == null && f.Metrics.Any(m => m.Metric == EnumMetrics.Kcal && m.Value > 0))
            .OrderBy(f => f.Id)
            .Take(FoodPoolSize)
            .Select(f => new
            {
                f.Id,
                Kcal = f.Metrics.Where(m => m.Metric == EnumMetrics.Kcal).Select(m => m.Value).First(),
                Weight = f.Metrics.Where(m => m.Metric == EnumMetrics.Weight).Select(m => (double?)m.Value)
                    .FirstOrDefault()
            })
            .ToListAsync();

        if (pool.Count < 8)
            throw new Exceptions.BadRequestException("seed_not_enough_foods");

        var existingDailies = await dbContext.UserDailies
            .Where(d => d.UserId == userId && d.Date >= from && d.Date < to &&
                        (d.Metric == EnumMetrics.Step || d.Metric == EnumMetrics.Water))
            .Select(d => new { d.Date, d.Metric })
            .ToListAsync();

        var menus = new List<DailyMenu>();
        var dailies = new List<UserDaily>();

        for (var i = 0; i < 14; i++)
        {
            var day = from.AddDays(i);
            var isLast = i >= 7;
            var k = i % 7;

            for (var m = 0; m < Meals.Length; m++)
            {
                // Gazak hamma kun emas.
                if (Meals[m].Menu == EnumMenu.Snack && i % 3 == 1) continue;

                // "Haftaning hiti": o'tgan haftaning Du, Ch, Ju, Ya tushligi — bitta taom.
                var food = isLast && Meals[m].Menu == EnumMenu.Lunch && k % 2 == 0
                    ? pool[0]
                    : pool[1 + (i * 4 + m + 1) % (pool.Count - 1)];

                var target = norm * Meals[m].Share * (isLast ? LastKcal[k] : PrevKcal[k]);
                var kcalPerGram = food.Kcal / (food.Weight is > 0 ? food.Weight.Value : DefaultFoodWeightMetric);

                menus.Add(new DailyMenu
                {
                    UserId = userId,
                    Date = day,
                    Menu = Meals[m].Menu,
                    FoodId = food.Id,
                    Weight = (int)Math.Clamp(Math.Round(target / kcalPerGram), 20, 1500)
                });
            }

            AddDaily(EnumMetrics.Step, isLast ? LastSteps[k] : PrevSteps[k]);
            AddDaily(EnumMetrics.Water, isLast ? LastWater[k] : PrevWater[k]);
            continue;

            void AddDaily(EnumMetrics metric, double value)
            {
                if (existingDailies.Any(d => d.Date == day && d.Metric == metric)) return;
                dailies.Add(new UserDaily { UserId = userId, Date = day, Metric = metric, Value = value });
            }
        }

        await dbContext.Transactional(async () =>
        {
            dbContext.DailyMenus.AddRange(menus);
            dbContext.UserDailies.AddRange(dailies);
            await dbContext.SaveChangesAsync();
        });

        return new SeedResultDto(userId, from, to.AddDays(-1), menus.Count, dailies.Count);
    }

    /// <summary>
    /// Seed qo'shganini o'chiradi: davrdagi barcha ovqatlar va qiymati seed bilan aynan bir xil qolgan qadam/suv.
    /// </summary>
    public async Task<SeedResultDto> Clear(string phone, DateTime? weekStart)
    {
        var userId = await FindUser(phone);
        var (from, to) = Period(weekStart);

        var meals = await dbContext.DailyMenus
            .Where(x => x.UserId == userId && x.Date >= from && x.Date < to)
            .ExecuteDeleteAsync();

        var rows = await dbContext.UserDailies
            .Where(d => d.UserId == userId && d.Date >= from && d.Date < to &&
                        (d.Metric == EnumMetrics.Step || d.Metric == EnumMetrics.Water))
            .ToListAsync();

        var seeded = rows.Where(d =>
        {
            var i = (int)(d.Date.Date - from).TotalDays;
            if (i is < 0 or >= 14) return false;
            var isLast = i >= 7;
            var k = i % 7;
            var value = d.Metric == EnumMetrics.Step
                ? isLast ? LastSteps[k] : PrevSteps[k]
                : isLast ? LastWater[k] : PrevWater[k];
            return Math.Abs(d.Value - value) < 0.0001;
        }).ToList();

        dbContext.UserDailies.RemoveRange(seeded);
        await dbContext.SaveChangesAsync();

        return new SeedResultDto(userId, from, to.AddDays(-1), meals, seeded.Count);
    }

    /// <summary>Telefon raqamining oxirgi 9 raqami bo'yicha — aynan bitta user topilishi shart.</summary>
    private async Task<long> FindUser(string phone)
    {
        var digits = new string(phone.Where(char.IsDigit).ToArray());
        if (digits.Length < 9)
            throw new Exceptions.BadRequestException("seed_phone_invalid");

        var tail = digits[^9..];
        var ids = await dbContext.Users
            .Where(u => u.Phone != null && u.Phone.EndsWith(tail))
            .Select(u => u.Id)
            .Take(2)
            .ToListAsync();

        return ids.Count == 1 ? ids[0] : throw new Exceptions.BadRequestException("seed_user_not_unique");
    }

    /// <summary>[hisobot haftasidan oldingi dushanba, hisobot haftasidan keyingi dushanba).</summary>
    private static (DateTime From, DateTime To) Period(DateTime? weekStart)
    {
        var date = weekStart?.Date ?? DateTime.Now.Date.AddDays(-7);
        var monday = date.AddDays(-(((int)date.DayOfWeek + 6) % 7));
        return (monday.AddDays(-7), monday.AddDays(7));
    }
}
