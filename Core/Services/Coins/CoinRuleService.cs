using BRB.Core.EF.Attributes;
using Core.Brokers.DbContext;
using Core.Entities.Coins;
using Core.Entities.Coins.Enum;
using Core.Enums;
using Core.Services.Coins.Contracts;
using Core.Services.Coins.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Core.Services.Coins;

/// <summary>
/// Coin sozlamalari (dashboard'dan boshqariladi):
/// <list type="bullet">
/// <item>Qadam → coin qoidalari (har N qadam = 1 coin, kunlik limit). Qoidalar sana bo'yicha tarix sifatida
/// saqlanadi: har kun o'sha kunda amal qilgan qoida bilan hisoblanadi, shuning uchun qoidani
/// o'zgartirish o'tgan kunlarga orqaga qarab coin qo'shmaydi. Faqat bugungi va kelajakdagi
/// qoidalarni o'zgartirish mumkin.</item>
/// <item>Coin hisoblash kuni (<see cref="CoinSetting"/>) va barcha coinlarni o'chirish (<see cref="CoinReset"/>).</item>
/// </list>
/// </summary>
[Injectable]
public class CoinRuleService(AppDbContext dbContext, IOptions<CoinConfig> options)
{
    /// <summary>Simulyatsiya uchun eng ko'p kun.</summary>
    private const int MaxPreviewDays = 90;

    private CoinConfig Config => options.Value;
    private CoinRuleSet? _ruleSet;
    private DateTime? _earnStart;

    /// <summary>
    /// Coin hisoblash boshlanadigan kun: dashboard sozlamasi, bo'lmasa <c>CoinConfig.CoinsEarnStartDate</c>.
    /// <paramref name="fresh"/> — so'rov ichidagi keshni chetlab, bazadan qayta o'qish.
    /// </summary>
    public async Task<DateTime> GetEarnStartDate(bool fresh = false)
    {
        if (!fresh && _earnStart.HasValue) return _earnStart.Value;

        var date = await dbContext.CoinSettings
            .AsNoTracking()
            .OrderBy(x => x.Id)
            .Select(x => (DateTime?)x.EarnStartDate)
            .FirstOrDefaultAsync();

        _earnStart = (date ?? Config.CoinsEarnStartDate).Date;
        return _earnStart.Value;
    }

    /// <summary>Barcha qoidalar (so'rov davomida bir marta o'qiladi — jadval juda kichik).</summary>
    public async Task<CoinRuleSet> GetRuleSet()
    {
        if (_ruleSet is not null) return _ruleSet;

        var rules = await dbContext.CoinRules
            .AsNoTracking()
            .OrderBy(x => x.EffectiveFrom)
            .Select(x => new CoinRuleSnapshot(x.Id, x.StepsPerCoin, x.MaxDailyCoins, x.EffectiveFrom))
            .ToListAsync();

        return _ruleSet = new CoinRuleSet(BaseRule, rules);
    }

    private CoinRuleSnapshot BaseRule =>
        new(null, Config.StepsPerCoin, Config.MaxDailyCoins, Config.CoinsEarnStartDate.Date);

    #region Admin

    public async Task<AdminCoinRulesDto> GetAdminRules()
    {
        var today = DateTime.Now.Date;

        var rows = await dbContext.CoinRules
            .AsNoTracking()
            .OrderBy(x => x.EffectiveFrom)
            .Select(x => new
            {
                x.Id, x.StepsPerCoin, x.MaxDailyCoins, x.EffectiveFrom, x.Note, x.CreatedAt,
                CreatedBy = x.CreatedBy != null ? x.CreatedBy.Name : null
            })
            .ToListAsync();

        // Boshlang'ich (appsettings) qoida — birinchi DB qoidasidan oldingi kunlar uchun.
        var all = new List<AdminCoinRuleDto>();
        var baseRule = BaseRule;
        if (rows.Count == 0 || rows[0].EffectiveFrom.Date > baseRule.EffectiveFrom)
        {
            all.Add(new AdminCoinRuleDto
            {
                Id = null,
                StepsPerCoin = baseRule.StepsPerCoin,
                MaxDailyCoins = baseRule.MaxDailyCoins,
                EffectiveFrom = baseRule.EffectiveFrom,
                IsDefault = true,
                Status = "",
                Editable = false
            });
        }

        all.AddRange(rows.Select(x => new AdminCoinRuleDto
        {
            Id = x.Id,
            StepsPerCoin = x.StepsPerCoin,
            MaxDailyCoins = x.MaxDailyCoins,
            EffectiveFrom = x.EffectiveFrom.Date,
            Note = x.Note,
            CreatedBy = x.CreatedBy,
            CreatedAt = x.CreatedAt,
            Status = "",
            Editable = x.EffectiveFrom.Date >= today
        }));

        // Holat va amal qilish oralig'i.
        var currentIndex = all.FindLastIndex(x => x.EffectiveFrom <= today);
        var result = all.Select((x, i) => x with
        {
            EffectiveTo = i + 1 < all.Count ? all[i + 1].EffectiveFrom.AddDays(-1) : null,
            Status = i == currentIndex ? "Current" : x.EffectiveFrom > today ? "Upcoming" : "Past"
        }).ToList();

        var current = currentIndex >= 0 ? result[currentIndex] : result[0];

        return new AdminCoinRulesDto
        {
            Current = current,
            Next = result.FirstOrDefault(x => x.EffectiveFrom > today),
            History = Enumerable.Reverse(result).ToList(),
            CoinsEarnStartDate = await GetEarnStartDate(),
            Today = today
        };
    }

    /// <summary>
    /// Qoidani saqlaydi: <see cref="SaveCoinRuleDto.EffectiveFrom"/> kunidan boshlab (bugun yoki kelajak).
    /// O'sha kunga qoida bo'lsa — yangilanadi.
    /// </summary>
    public async Task<AdminCoinRulesDto> Save(SaveCoinRuleDto dto, long adminId)
    {
        var today = DateTime.Now.Date;
        var day = (dto.EffectiveFrom ?? today).Date;

        if (day < today)
            throw new CoinRulePastDateException();

        var rule = await dbContext.CoinRules.FirstOrDefaultAsync(x => x.EffectiveFrom == day);
        if (rule is null)
        {
            rule = dbContext.CoinRules.Add(new CoinRule { EffectiveFrom = day }).Entity;
        }

        rule.StepsPerCoin = dto.StepsPerCoin;
        rule.MaxDailyCoins = dto.MaxDailyCoins;
        rule.Note = string.IsNullOrWhiteSpace(dto.Note) ? null : dto.Note.Trim();
        rule.CreatedById = adminId;

        await dbContext.SaveChangesAsync();
        _ruleSet = null;

        return await GetAdminRules();
    }

    /// <summary>Faqat bugungi yoki kelajakdagi qoidani o'chirish mumkin — o'tgan kunlar tarixi o'zgarmaydi.</summary>
    public async Task<AdminCoinRulesDto> Delete(long ruleId)
    {
        var rule = await dbContext.CoinRules.FirstOrDefaultAsync(x => x.Id == ruleId)
                   ?? throw new CoinRuleNotFoundException();

        if (rule.EffectiveFrom.Date < DateTime.Now.Date)
            throw new CoinRuleLockedException();

        dbContext.CoinRules.Remove(rule);
        await dbContext.SaveChangesAsync();
        _ruleSet = null;

        return await GetAdminRules();
    }

    /// <summary>Coin hisoblash kuni, hozirgi coinlar holati va reset tarixi.</summary>
    public async Task<AdminCoinEarnStartDto> GetEarnStart()
    {
        var setting = await dbContext.CoinSettings
            .AsNoTracking()
            .OrderBy(x => x.Id)
            .Select(x => new
            {
                x.EarnStartDate, x.UpdatedAt,
                UpdatedBy = x.UpdatedBy != null ? x.UpdatedBy.Name : null
            })
            .FirstOrDefaultAsync();

        var start = (setting?.EarnStartDate ?? Config.CoinsEarnStartDate).Date;
        var startRef = start.Year * 10000L + start.Month * 100 + start.Day;
        var steps = EnumCoinTxType.Steps;

        var wallets = dbContext.CoinWallets.AsNoTracking();

        var resets = await dbContext.CoinResets
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAt)
            .Take(20)
            .Select(x => new AdminCoinResetDto
            {
                Id = x.Id,
                CreatedAt = x.CreatedAt,
                CreatedBy = x.CreatedBy != null ? x.CreatedBy.Name : null,
                EarnStartDate = x.EarnStartDate,
                UsersAffected = x.UsersAffected,
                BalanceRemoved = x.BalanceRemoved,
                EarnedRemoved = x.EarnedRemoved,
                TransactionsRemoved = x.TransactionsRemoved
            })
            .ToListAsync();

        var earliestRef = await dbContext.CoinTransactions
            .Where(x => x.Type == steps && x.RefId > 0)
            .MinAsync(x => x.RefId);

        return new AdminCoinEarnStartDto
        {
            EarnStartDate = start,
            IsDefault = setting is null,
            UpdatedBy = setting?.UpdatedBy,
            UpdatedAt = setting?.UpdatedAt,
            Today = DateTime.Now.Date,
            WalletsWithCoins = await wallets.CountAsync(x => x.Balance != 0 || x.TotalEarned != 0 || x.TotalSpent != 0),
            Balance = await wallets.SumAsync(x => x.Balance),
            Earned = await wallets.SumAsync(x => x.TotalEarned),
            Transactions = await dbContext.CoinTransactions.CountAsync(),
            EarliestStepDay = earliestRef is > 0
                ? new DateTime((int)(earliestRef.Value / 10000), (int)(earliestRef.Value / 100 % 100), (int)(earliestRef.Value % 100))
                : null,
            StepCoinsBeforeStart = await dbContext.CoinTransactions
                .Where(x => x.Type == steps && x.RefId < startRef)
                .SumAsync(x => x.Amount),
            Resets = resets
        };
    }

    /// <summary>
    /// Coin hisoblash kunini o'rnatadi. <see cref="SaveCoinEarnStartDto.ResetCoins"/> bo'lsa barcha userlarning
    /// coin balansi va coin tarixi o'chiriladi (do'kon xaridlari va berilgan Premium qoladi) — keyin coin faqat
    /// yangi kundan boshlab qayta yig'iladi. Reset'siz faqat kun o'zgaradi: sana oldinga surilsa eski kunlar
    /// coini qoladi, orqaga surilsa o'sha kunlar uchun coin qo'shiladi.
    /// </summary>
    public async Task<AdminCoinEarnStartDto> SaveEarnStart(SaveCoinEarnStartDto dto, long adminId)
    {
        var day = dto.EarnStartDate.Date;
        var today = DateTime.Now.Date;
        if (day < new DateTime(2020, 1, 1) || day > today.AddYears(1))
            throw new CoinEarnStartInvalidException();

        await dbContext.Transactional(async () =>
        {
            // SyncStepCoins hamyon qatorini FOR UPDATE bilan qulflaydi — bu qulf parallel sinxronlarni shu
            // tranzaksiya tugaguncha kutdiradi, ular keyin yangi kunni o'qiydi (eski coin qayta yozilmaydi).
            await dbContext.Database.ExecuteSqlRawAsync("lock table coin_wallets in exclusive mode");

            var setting = await dbContext.CoinSettings.OrderBy(x => x.Id).FirstOrDefaultAsync()
                          ?? dbContext.CoinSettings.Add(new CoinSetting()).Entity;
            setting.EarnStartDate = day;
            setting.UpdatedById = adminId;

            if (dto.ResetCoins)
            {
                var wallets = dbContext.CoinWallets;
                var users = await wallets.CountAsync(x => x.Balance != 0 || x.TotalEarned != 0 || x.TotalSpent != 0);
                var balance = await wallets.SumAsync(x => x.Balance);
                var earned = await wallets.SumAsync(x => x.TotalEarned);

                var removed = await dbContext.CoinTransactions.ExecuteDeleteAsync();

                var now = DateTime.Now;
                await wallets.ExecuteUpdateAsync(s => s
                    .SetProperty(x => x.Balance, 0)
                    .SetProperty(x => x.TotalEarned, 0)
                    .SetProperty(x => x.TotalSpent, 0)
                    .SetProperty(x => x.UpdatedAt, now));

                dbContext.CoinResets.Add(new CoinReset
                {
                    EarnStartDate = day,
                    UsersAffected = users,
                    BalanceRemoved = balance,
                    EarnedRemoved = earned,
                    TransactionsRemoved = removed,
                    CreatedById = adminId
                });
            }

            await dbContext.SaveChangesAsync();
        });

        _earnStart = null;
        return await GetEarnStart();
    }

    /// <summary>
    /// Oxirgi <paramref name="days"/> kun (bugun kirmaydi — u hali tugamagan) userlarning haqiqiy qadamlari
    /// bo'yicha joriy va taklif qilingan qoida qancha coin berishini hisoblaydi.
    /// </summary>
    public async Task<CoinRulePreviewDto> Preview(int stepsPerCoin, int maxDailyCoins, int days = 30)
    {
        if (stepsPerCoin < 1 || maxDailyCoins < 1)
            throw new CoinRuleInvalidException();

        var (from, to, samples) = await LoadDailySteps(days);
        days = (to - from).Days;

        var steps = samples.Select(x => x.Steps).OrderBy(x => x).ToList();
        var current = (await GetRuleSet()).Today;

        return new CoinRulePreviewDto
        {
            From = from,
            To = to.AddDays(-1),
            Days = days,
            Walkers = samples.Select(x => x.UserId).Distinct().Count(),
            WalkerDays = steps.Count,
            AvgDailySteps = steps.Count > 0 ? Math.Round(steps.Average()) : 0,
            MedianDailySteps = steps.Count > 0 ? Math.Round(steps[steps.Count / 2]) : 0,
            Current = Impact(steps, current.StepsPerCoin, current.MaxDailyCoins, days),
            Proposed = Impact(steps, stepsPerCoin, maxDailyCoins, days)
        };
    }

    /// <summary>
    /// Faol (kuniga ≥1 coin oladigan) user joriy qoida bo'yicha bir kunda o'rtacha yig'adigan coin —
    /// do'kon narxini "necha kunda yig'iladi" ga aylantirish uchun.
    /// </summary>
    public async Task<double> AvgCoinsPerEarningDay(int days = 30)
    {
        var (from, to, samples) = await LoadDailySteps(days);
        var current = (await GetRuleSet()).Today;

        return Impact(samples.Select(x => x.Steps).ToList(), current.StepsPerCoin, current.MaxDailyCoins,
            (to - from).Days).AvgCoinsPerEarningDay;
    }

    private record DailySteps(long UserId, double Steps);

    /// <summary>Har user-kun uchun qadam (o'sha kungi eng katta qiymat), bugun kirmaydi.</summary>
    private async Task<(DateTime From, DateTime To, List<DailySteps> Samples)> LoadDailySteps(int days)
    {
        days = Math.Clamp(days, 1, MaxPreviewDays);
        var to = DateTime.Now.Date;
        var from = to.AddDays(-days);

        var samples = await dbContext.UserDailies
            .AsNoTracking()
            .Where(x => x.Metric == EnumMetrics.Step && x.Date >= from && x.Date < to && x.Value > 0)
            .Where(x => !x.User.IsDeleted)
            .GroupBy(x => new { x.UserId, Day = x.Date.Date })
            .Select(g => new DailySteps(g.Key.UserId, g.Max(x => x.Value)))
            .ToListAsync();

        return (from, to, samples);
    }

    private static CoinRuleImpactDto Impact(IReadOnlyCollection<double> steps, int perCoin, int cap, int days)
    {
        var coins = steps.Select(s => perCoin <= 0 ? 0 : Math.Min((long)(s / perCoin), cap)).ToList();
        var total = coins.Sum();
        var earning = coins.Count(c => c > 0);

        return new CoinRuleImpactDto
        {
            StepsPerCoin = perCoin,
            MaxDailyCoins = cap,
            TotalCoins = total,
            CoinsPerDay = days > 0 ? Math.Round(total / (double)days, 1) : 0,
            AvgCoinsPerWalkerDay = coins.Count > 0 ? Math.Round(total / (double)coins.Count, 2) : 0,
            CappedShare = coins.Count > 0 ? Math.Round(coins.Count(c => c >= cap) / (double)coins.Count, 4) : 0,
            EarningShare = coins.Count > 0 ? Math.Round(earning / (double)coins.Count, 4) : 0,
            AvgCoinsPerEarningDay = earning > 0 ? Math.Round(total / (double)earning, 2) : 0
        };
    }

    #endregion
}
