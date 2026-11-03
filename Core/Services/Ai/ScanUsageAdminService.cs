using BRB.Core.Common.Models;
using BRB.Core.EF.Attributes;
using BRB.Core.EF.Extensions;
using Core.Brokers.DbContext;
using Core.Enums;
using Core.Services.User.Exceptions;
using Core.Services.Ai.Contracts;
using Microsoft.EntityFrameworkCore;
using ResultWrapper.Library;

namespace Core.Services.Ai;

/// <summary>
/// Dashboard: kimda nechta bepul AI skan bor, qanchasi ishlatilgan va skanlar jurnali.
/// Limit qoidasi <see cref="AiQuotaService"/> bilan bir xil: Premium — cheksiz,
/// Free — faol Free tarifning AiScans qiymati + do'kon bonusi.
/// </summary>
[Injectable]
public class ScanUsageAdminService(AppDbContext dbContext, AiQuotaService aiQuotaService)
{
    private const string Source = AiService.EventSource;
    private const int EventsTake = 100;

    private sealed class ScanRow
    {
        public long UserId { get; init; }
        public string Name { get; init; } = null!;
        public string? Email { get; init; }
        public string? Phone { get; init; }
        public string? Photo { get; init; }
        public DateTime RegisteredAt { get; init; }
        public EnumSPlans? Plan { get; init; }
        public DateTime? PlanEndsAt { get; init; }
        public int Used { get; init; }
        public int Bonus { get; init; }
        public int TotalScans { get; init; }
        public int SuccessScans { get; init; }
        public int ErrorScans { get; init; }
        public DateTime? LastScanAt { get; init; }
    }

    private IQueryable<ScanRow> Rows()
    {
        var feature = AiQuotaService.Feature;

        return dbContext.Users
            .AsNoTracking()
            .Select(u => new ScanRow
            {
                UserId = u.Id,
                Name = u.Name,
                Email = u.Email,
                Phone = u.Phone,
                Photo = u.Extra != null ? u.Extra.Photo : null,
                RegisteredAt = u.CreatedAt,
                Plan = dbContext.Subscriptions
                    .Where(s => s.UserId == u.Id && s.IsActive)
                    .Select(s => (EnumSPlans?)s.SubscriptionPlan)
                    .FirstOrDefault(),
                PlanEndsAt = dbContext.Subscriptions
                    .Where(s => s.UserId == u.Id && s.IsActive)
                    .Select(s => (DateTime?)s.EndsAt)
                    .FirstOrDefault(),
                Used = dbContext.UserFeatureUsages
                    .Where(f => f.UserId == u.Id && f.FeatureKey == feature)
                    .Select(f => f.Used)
                    .FirstOrDefault(),
                Bonus = dbContext.UserFeatureUsages
                    .Where(f => f.UserId == u.Id && f.FeatureKey == feature)
                    .Select(f => f.BonusLimit)
                    .FirstOrDefault(),
                TotalScans = dbContext.EventLogs.Count(e => e.UserId == u.Id && e.Source == Source),
                SuccessScans = dbContext.EventLogs.Count(e =>
                    e.UserId == u.Id && e.Source == Source && e.Status == EnumEventStatus.Success),
                ErrorScans = dbContext.EventLogs.Count(e =>
                    e.UserId == u.Id && e.Source == Source && e.Status == EnumEventStatus.Error),
                LastScanAt = dbContext.EventLogs
                    .Where(e => e.UserId == u.Id && e.Source == Source)
                    .Max(e => (DateTime?)e.CreatedAt)
            });
    }

    private static AdminScanUserDto ToDto(ScanRow x, bool freeUnlimited, int freeLimit)
    {
        var isPremium = x.Plan != null && x.Plan != EnumSPlans.Free;
        var unlimited = isPremium || freeUnlimited;
        var limit = freeLimit + x.Bonus;

        return new AdminScanUserDto
        {
            UserId = x.UserId,
            Name = x.Name,
            Email = x.Email,
            Phone = x.Phone,
            Photo = x.Photo,
            RegisteredAt = x.RegisteredAt,
            Plan = x.Plan,
            IsPremium = isPremium,
            PremiumEndsAt = isPremium ? x.PlanEndsAt : null,
            Unlimited = unlimited,
            FreeLimit = freeLimit,
            BonusLimit = x.Bonus,
            Limit = limit,
            Used = x.Used,
            Remaining = unlimited ? 0 : Math.Max(0, limit - x.Used),
            TotalScans = x.TotalScans,
            SuccessScans = x.SuccessScans,
            ErrorScans = x.ErrorScans,
            LastScanAt = x.LastScanAt
        };
    }

    public async Task<AdminScanSummaryDto> GetSummary()
    {
        var feature = AiQuotaService.Feature;
        var (freeUnlimited, freeLimit) = await aiQuotaService.ResolvePlanFeatureLimit(EnumSPlans.Free);

        var freePlans = await dbContext.PlanExtras
            .AsNoTracking()
            .Where(p => p.Plan == EnumSPlans.Free && p.IsActive)
            .Select(p => p.Features.Any(f => f.FeatureKey == feature))
            .ToListAsync();

        var totalUsers = await dbContext.Users.CountAsync();
        var premiumUsers = await dbContext.Users.CountAsync(u => dbContext.Subscriptions.Any(s =>
            s.UserId == u.Id && s.IsActive && s.SubscriptionPlan != EnumSPlans.Free));

        var usages = await dbContext.UserFeatureUsages
            .AsNoTracking()
            .Where(f => f.FeatureKey == feature && !f.User.IsDeleted)
            .Select(f => new
            {
                f.Used,
                f.BonusLimit,
                IsPremium = dbContext.Subscriptions.Any(s =>
                    s.UserId == f.UserId && s.IsActive && s.SubscriptionPlan != EnumSPlans.Free)
            })
            .ToListAsync();

        var freeUsages = usages.Where(x => !x.IsPremium).ToList();
        var freeUsers = totalUsers - premiumUsers;
        var freeScanned = freeUsages.Count(x => x.Used > 0);

        var distribution = freeUsages
            .Where(x => x.Used > 0)
            .GroupBy(x => x.Used)
            .Select(g => new AdminScanUsageBucketDto { Used = g.Key, Users = g.Count() })
            .Prepend(new AdminScanUsageBucketDto { Used = 0, Users = Math.Max(0, freeUsers - freeScanned) })
            .OrderBy(x => x.Used)
            .ToList();

        var byStatus = await dbContext.EventLogs
            .Where(e => e.Source == Source)
            .GroupBy(e => e.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Status, x => x.Count);

        var today = DateTime.Now.Date;

        return new AdminScanSummaryDto
        {
            FreeLimit = freeLimit,
            FreeUnlimited = freeUnlimited,
            FreePlanActive = freePlans.Count > 0,
            FreeLimitConfigured = freePlans.Any(x => x),
            TotalUsers = totalUsers,
            PremiumUsers = premiumUsers,
            FreeUsers = freeUsers,
            FreeUsersScanned = freeScanned,
            ExhaustedUsers = freeUnlimited ? 0 : freeUsages.Count(x => x.Used >= freeLimit + x.BonusLimit) +
                                                 (freeLimit <= 0 ? freeUsers - freeUsages.Count : 0),
            UsersWithBonus = usages.Count(x => x.BonusLimit > 0),
            BonusGranted = usages.Sum(x => (long)x.BonusLimit),
            FreeScansUsed = usages.Sum(x => (long)x.Used),
            TotalScans = byStatus.Values.Sum(),
            SuccessScans = byStatus.GetValueOrDefault(EnumEventStatus.Success),
            EmptyScans = byStatus.GetValueOrDefault(EnumEventStatus.Warning),
            ErrorScans = byStatus.GetValueOrDefault(EnumEventStatus.Error),
            ScansToday = await dbContext.EventLogs.CountAsync(e => e.Source == Source && e.CreatedAt >= today),
            Scanners = await dbContext.EventLogs
                .Where(e => e.Source == Source && e.UserId != null)
                .Select(e => e.UserId)
                .Distinct()
                .CountAsync(),
            Distribution = distribution
        };
    }

    /// <summary>
    /// Userlar ro'yxati. <c>status</c>: scanned (skan qilgan yoki bepul skan ishlatgan) | exhausted (limiti tugagan) |
    /// available (bepul skani qolgan) | free | premium | bonus | all.
    /// <c>sort</c>: used | scans | remaining | recent | newest. <c>search</c> — ism, email, telefon yoki user id.
    /// </summary>
    public async Task<Wrapper> GetUsers(string? status, string? sort, string? search, DataQueryRequest q)
    {
        var (freeUnlimited, freeLimit) = await aiQuotaService.ResolvePlanFeatureLimit(EnumSPlans.Free);
        var query = Rows();

        query = status?.ToLowerInvariant() switch
        {
            "all" => query,
            "free" => query.Where(x => x.Plan == null || x.Plan == EnumSPlans.Free),
            "premium" => query.Where(x => x.Plan != null && x.Plan != EnumSPlans.Free),
            "bonus" => query.Where(x => x.Bonus > 0),
            "exhausted" => freeUnlimited
                ? query.Where(x => false)
                : query.Where(x => (x.Plan == null || x.Plan == EnumSPlans.Free) && x.Used >= freeLimit + x.Bonus),
            "available" => query.Where(x =>
                (x.Plan == null || x.Plan == EnumSPlans.Free) && (freeUnlimited || x.Used < freeLimit + x.Bonus)),
            _ => query.Where(x => x.TotalScans > 0 || x.Used > 0)
        };

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            var pattern = $"%{term}%";
            var id = long.TryParse(term, out var parsed) ? parsed : 0;

            query = query.Where(x => x.UserId == id ||
                                     EF.Functions.ILike(x.Name, pattern) ||
                                     EF.Functions.ILike(x.Email!, pattern) ||
                                     EF.Functions.ILike(x.Phone!, pattern));
        }

        var sorted = sort?.ToLowerInvariant() switch
        {
            "scans" => query.OrderByDescending(x => x.TotalScans).ThenByDescending(x => x.Used),
            "remaining" => query
                .OrderBy(x => x.Plan != null && x.Plan != EnumSPlans.Free ? 1 : 0)
                .ThenBy(x => freeLimit + x.Bonus - x.Used),
            "recent" => query.OrderByDescending(x => x.LastScanAt.HasValue).ThenByDescending(x => x.LastScanAt),
            "newest" => query.OrderByDescending(x => x.RegisteredAt),
            _ => query.OrderByDescending(x => x.Used).ThenByDescending(x => x.TotalScans)
        };

        var total = await query.CountAsync();
        var rows = await sorted
            .ThenByDescending(x => x.UserId)
            .Page(q)
            .ToListAsync();

        return (rows.Select(x => ToDto(x, freeUnlimited, freeLimit)).ToList(), total);
    }

    public async Task<AdminScanUserDetailDto> GetUser(long userId)
    {
        var (freeUnlimited, freeLimit) = await aiQuotaService.ResolvePlanFeatureLimit(EnumSPlans.Free);

        var row = await Rows().FirstOrDefaultAsync(x => x.UserId == userId)
                  ?? throw new UserNotFoundException();

        var events = await dbContext.EventLogs
            .AsNoTracking()
            .Where(e => e.UserId == userId && e.Source == Source)
            .OrderByDescending(e => e.CreatedAt)
            .ThenByDescending(e => e.Id)
            .Take(EventsTake)
            .Select(e => new { e.Id, e.CreatedAt, e.Outcome, e.Status, e.DurationMs, e.ErrorMessage, e.Metadata })
            .ToListAsync();

        return new AdminScanUserDetailDto
        {
            User = ToDto(row, freeUnlimited, freeLimit),
            Events = events.Select(e => new AdminScanEventDto
            {
                Id = e.Id,
                CreatedAt = e.CreatedAt,
                Outcome = e.Outcome,
                Status = e.Status,
                DurationMs = e.DurationMs,
                ItemCount = e.Metadata != null && e.Metadata.TryGetValue("itemCount", out var c) &&
                            int.TryParse(c, out var n)
                    ? n
                    : null,
                MimeType = e.Metadata?.GetValueOrDefault("mimeType"),
                ErrorMessage = e.ErrorMessage
            }).ToList()
        };
    }
}
