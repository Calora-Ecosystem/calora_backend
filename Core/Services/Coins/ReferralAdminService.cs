using BRB.Core.Common.Models;
using BRB.Core.EF.Attributes;
using BRB.Core.EF.Extensions;
using Core.Brokers.DbContext;
using Core.Entities.Billing.Enum;
using Core.Entities.Coins.Enum;
using Core.Helpers;
using Core.Services.Coins.Contracts;
using Core.Services.User.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ResultWrapper.Library;

namespace Core.Services.Coins;

/// <summary>
/// Dashboard: "Do'stni taklif qilish" dasturini to'liq kuzatish — voronka (kod → do'st → faol → to'lov),
/// taklif qiluvchilar reytingi, kim kimni taklif qilgani, tushum va chegirmalar.
/// <para>
/// Kogorta — davrda kod kiritgan do'stlar; ularning keyingi natijasi (faol bo'lishi, to'lovi) davrdan
/// keyin bo'lsa ham hisoblanadi. To'lov — kod kiritilgandan keyingi tasdiqlangan buyurtmalar
/// (<c>orders.amount</c> tiyinda, javobda so'm).
/// </para>
/// </summary>
[Injectable]
public class ReferralAdminService(AppDbContext dbContext, IOptions<CoinConfig> options)
{
    private const int MaxDays = 366;
    private const int DetailDays = 90;

    private CoinConfig Config => options.Value;
    private int Goal => Math.Max(1, Config.ReferralPremiumFriends);

    private record RefRow(long Id, long ReferrerId, long ReferredUserId, DateTime CreatedAt, DateTime? QualifiedAt);

    private record OrderRow(long UserId, long Amount, DateTime CreatedAt);

    private record UserRow(long Id, string? Name, string? Email, string? Phone, DateTime CreatedAt, bool IsDeleted);

    #region Yordamchilar

    private async Task<(DateTime From, DateTime To)> Period(DateTime? from, DateTime? to)
    {
        var first = await dbContext.Referrals.MinAsync(x => (DateTime?)x.CreatedAt);
        var start = first.HasValue && first.Value.Date < Config.CoinsEarnStartDate.Date
            ? first.Value.Date
            : Config.CoinsEarnStartDate.Date;

        return DashboardPeriod.Resolve(from, to, start);
    }

    /// <summary>Davrda kod kiritilgan takliflar; davr null bo'lsa — butun vaqt.</summary>
    private Task<List<RefRow>> LoadReferrals(DateTime? start, DateTime? end, long? referrerId = null) =>
        dbContext.Referrals
            .AsNoTracking()
            .Where(x => start == null || x.CreatedAt >= start)
            .Where(x => end == null || x.CreatedAt <= end)
            .Where(x => referrerId == null || x.ReferrerId == referrerId)
            .Select(x => new RefRow(x.Id, x.ReferrerId, x.ReferredUserId, x.CreatedAt, x.QualifiedAt))
            .ToListAsync();

    /// <summary>Userlarning tasdiqlangan buyurtmalari (eskidan yangiga).</summary>
    private async Task<Dictionary<long, List<OrderRow>>> LoadOrders(IEnumerable<long> userIds)
    {
        var ids = userIds.Distinct().ToList();
        if (ids.Count == 0) return [];

        return (await dbContext.Orders
                .AsNoTracking()
                .Where(x => x.Status == EnumOrderStatus.Confirmed && ids.Contains(x.UserId))
                .Select(x => new OrderRow(x.UserId, x.Amount, x.CreatedAt))
                .ToListAsync())
            .GroupBy(x => x.UserId)
            .ToDictionary(g => g.Key, g => g.OrderBy(x => x.CreatedAt).ToList());
    }

    /// <summary>Taklif qilingan userning kod kiritgandan keyingi to'lovlari.</summary>
    private static List<OrderRow> OrdersAfter(Dictionary<long, List<OrderRow>> orders, RefRow r) =>
        orders.TryGetValue(r.ReferredUserId, out var list)
            ? list.Where(o => o.CreatedAt >= r.CreatedAt).ToList()
            : [];

    private static double Som(long tiyin) => Math.Round(tiyin / 100d, 2);

    private static double Rate(int part, int whole) => whole > 0 ? Math.Round(part / (double)whole, 4) : 0;

    /// <summary>O'chirilgan userlar ham (tarix uchun).</summary>
    private async Task<Dictionary<long, UserRow>> LoadUsers(IEnumerable<long> userIds)
    {
        var ids = userIds.Distinct().ToList();
        if (ids.Count == 0) return [];

        return await dbContext.Users
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x => ids.Contains(x.Id))
            .Select(x => new UserRow(x.Id, x.Name, x.Email, x.Phone, x.CreatedAt, x.IsDeleted))
            .ToDictionaryAsync(x => x.Id);
    }

    private async Task<Dictionary<long, string?>> LoadPhotos(IEnumerable<long> userIds)
    {
        var ids = userIds.Distinct().ToList();
        if (ids.Count == 0) return [];

        return (await dbContext.UserExtras
                .AsNoTracking()
                .Where(x => ids.Contains(x.UserId))
                .Select(x => new { x.UserId, x.Photo })
                .ToListAsync())
            .DistinctBy(x => x.UserId)
            .ToDictionary(x => x.UserId, x => x.Photo);
    }

    private async Task<HashSet<long>> LoadPremium(IEnumerable<long> userIds)
    {
        var ids = userIds.Distinct().ToList();
        if (ids.Count == 0) return [];

        var now = DateTime.Now;
        return (await dbContext.Subscriptions
                .AsNoTracking()
                .Where(x => ids.Contains(x.UserId) && x.IsActive && x.EndsAt > now)
                .Select(x => x.UserId)
                .ToListAsync())
            .ToHashSet();
    }

    private static string? Contact(UserRow? u) => u?.Phone ?? u?.Email;

    private ReferralProgramDto Program => new()
    {
        FriendsGoal = Goal,
        PremiumDays = Config.ReferralPremiumDays,
        DiscountPercent = Config.ReferredDiscountPercent,
        ReferrerReward = Config.ReferralReward,
        ReferredReward = Config.ReferredReward,
        ApplyWindowDays = Config.ReferralApplyWindowDays
    };

    #endregion

    #region Umumiy ko'rsatkichlar

    public async Task<AdminReferralSummaryDto> GetSummary(DateTime? from, DateTime? to)
    {
        var (start, end) = await Period(from, to);

        var refs = await LoadReferrals(start, end);
        var orders = await LoadOrders(refs.Select(x => x.ReferredUserId));

        var paidRefs = refs
            .Select(r => new { Ref = r, Orders = OrdersAfter(orders, r) })
            .Where(x => x.Orders.Count > 0)
            .ToList();

        var invited = refs.Count;
        var activated = refs.Count(x => x.QualifiedAt != null);
        var referrers = refs.Select(x => x.ReferrerId).Distinct().Count();

        var codes = await dbContext.ReferralCodes
            .AsNoTracking()
            .Where(x => x.CreatedAt >= start && x.CreatedAt <= end)
            .GroupBy(x => x.UserId)
            .Select(g => g.Count())
            .ToListAsync();

        var newUsers = await dbContext.Users.CountAsync(x => x.CreatedAt >= start && x.CreatedAt <= end);
        var newUsersReferred = await dbContext.Users.CountAsync(x =>
            x.CreatedAt >= start && x.CreatedAt <= end &&
            dbContext.Referrals.Any(r => r.ReferredUserId == x.Id));

        var discountsUsed = await dbContext.Referrals
            .CountAsync(x => x.DiscountUsedAt >= start && x.DiscountUsedAt <= end);
        var discountGiven = await dbContext.Orders
            .Where(x => x.Status == EnumOrderStatus.Confirmed && x.ReferralDiscount > 0 &&
                        x.CreatedAt >= start && x.CreatedAt <= end)
            .SumAsync(x => x.ReferralDiscount);

        var grants = await dbContext.ReferralPremiumGrants
            .AsNoTracking()
            .Where(x => x.CreatedAt >= start && x.CreatedAt <= end)
            .Select(x => new { x.Days, x.CreatedAt })
            .ToListAsync();

        var coinsRewarded = await dbContext.CoinTransactions
            .Where(x => x.Type == EnumCoinTxType.Referral && x.Amount > 0 &&
                        x.CreatedAt >= start && x.CreatedAt <= end)
            .SumAsync(x => x.Amount);

        var activeByReferrer = await dbContext.Referrals
            .Where(x => x.QualifiedAt != null)
            .GroupBy(x => x.ReferrerId)
            .Select(g => g.Count())
            .ToListAsync();

        var totalInvited = await dbContext.Referrals.CountAsync();
        var totalReferrers = await dbContext.Referrals.Select(x => x.ReferrerId).Distinct().CountAsync();
        var totalGrants = await dbContext.ReferralPremiumGrants.CountAsync();

        var activateHours = refs
            .Where(x => x.QualifiedAt != null)
            .Select(x => Math.Max(0, (x.QualifiedAt!.Value - x.CreatedAt).TotalHours))
            .ToList();

        var paymentDays = paidRefs
            .Select(x => Math.Max(0, (x.Orders[0].CreatedAt - x.Ref.CreatedAt).TotalDays))
            .ToList();

        var revenue = paidRefs.Sum(x => x.Orders.Sum(o => o.Amount));

        return new AdminReferralSummaryDto
        {
            From = start,
            To = end,
            CodesCreated = codes.Sum(),
            Sharers = codes.Count,
            Invited = invited,
            Activated = activated,
            Pending = invited - activated,
            Paid = paidRefs.Count,
            Referrers = referrers,
            AvgInvitesPerReferrer = referrers > 0 ? Math.Round(invited / (double)referrers, 2) : 0,
            CodeConversion = Rate(invited, codes.Sum()),
            ActivationRate = Rate(activated, invited),
            PaidRate = Rate(paidRefs.Count, invited),
            AvgHoursToActivate = activateHours.Count > 0 ? Math.Round(activateHours.Average(), 1) : null,
            AvgDaysToFirstPayment = paymentDays.Count > 0 ? Math.Round(paymentDays.Average(), 1) : null,
            NewUsers = newUsers,
            NewUsersReferred = newUsersReferred,
            ReferralShareOfNewUsers = Rate(newUsersReferred, newUsers),
            Revenue = Som(revenue),
            Orders = paidRefs.Sum(x => x.Orders.Count),
            RevenuePerPaid = paidRefs.Count > 0 ? Math.Round(Som(revenue) / paidRefs.Count, 2) : 0,
            DiscountsUsed = discountsUsed,
            DiscountGiven = Som(discountGiven),
            PremiumGrants = grants.Count,
            PremiumDaysGranted = grants.Sum(x => x.Days),
            CoinsRewarded = coinsRewarded,
            NearMilestone = Goal > 1 ? activeByReferrer.Count(c => c % Goal == Goal - 1) : 0,
            TotalInvited = totalInvited,
            TotalActivated = activeByReferrer.Sum(),
            TotalReferrers = totalReferrers,
            TotalPremiumGrants = totalGrants,
            Program = Program,
            Days = await BuildDays(start, end, null)
        };
    }

    /// <summary>
    /// Kunma-kun: yaratilgan kodlar, kod kiritganlar, faol bo'lganlar, birinchi to'lovlar va premiumlar.
    /// <paramref name="referrerId"/> berilsa — faqat shu taklif qiluvchi uchun.
    /// </summary>
    private async Task<List<AdminReferralDayDto>> BuildDays(DateTime start, DateTime end, long? referrerId)
    {
        var lastDay = end.Date < DateTime.Now.Date ? end.Date : DateTime.Now.Date;
        var firstDay = start.Date;
        if ((lastDay - firstDay).Days >= MaxDays)
            firstDay = lastDay.AddDays(1 - MaxDays);
        if (firstDay > lastDay) return [];

        var rangeEnd = lastDay.AddDays(1);

        var codes = (await dbContext.ReferralCodes
                .AsNoTracking()
                .Where(x => x.CreatedAt >= firstDay && x.CreatedAt < rangeEnd)
                .Where(x => referrerId == null || x.UserId == referrerId)
                .Select(x => x.CreatedAt)
                .ToListAsync())
            .GroupBy(x => x.Date)
            .ToDictionary(g => g.Key, g => g.Count());

        var invited = (await LoadReferrals(firstDay, rangeEnd, referrerId))
            .GroupBy(x => x.CreatedAt.Date)
            .ToDictionary(g => g.Key, g => g.Count());

        var activated = (await dbContext.Referrals
                .AsNoTracking()
                .Where(x => x.QualifiedAt >= firstDay && x.QualifiedAt < rangeEnd)
                .Where(x => referrerId == null || x.ReferrerId == referrerId)
                .Select(x => x.QualifiedAt!.Value)
                .ToListAsync())
            .GroupBy(x => x.Date)
            .ToDictionary(g => g.Key, g => g.Count());

        // Birinchi to'lov: kod kiritilgandan keyingi birinchi tasdiqlangan buyurtma o'sha kunga tushsa.
        var refsUpToEnd = await dbContext.Referrals
            .AsNoTracking()
            .Where(x => x.CreatedAt < rangeEnd)
            .Where(x => referrerId == null || x.ReferrerId == referrerId)
            .Select(x => new RefRow(x.Id, x.ReferrerId, x.ReferredUserId, x.CreatedAt, x.QualifiedAt))
            .ToListAsync();
        var orders = await LoadOrders(refsUpToEnd.Select(x => x.ReferredUserId));
        var firstPayments = refsUpToEnd
            .Select(r => OrdersAfter(orders, r).FirstOrDefault())
            .Where(o => o is not null && o.CreatedAt >= firstDay && o.CreatedAt < rangeEnd)
            .GroupBy(o => o!.CreatedAt.Date)
            .ToDictionary(g => g.Key, g => g.Count());

        var grants = (await dbContext.ReferralPremiumGrants
                .AsNoTracking()
                .Where(x => x.CreatedAt >= firstDay && x.CreatedAt < rangeEnd)
                .Where(x => referrerId == null || x.ReferrerId == referrerId)
                .Select(x => x.CreatedAt)
                .ToListAsync())
            .GroupBy(x => x.Date)
            .ToDictionary(g => g.Key, g => g.Count());

        var days = new List<AdminReferralDayDto>();
        for (var d = firstDay; d <= lastDay; d = d.AddDays(1))
        {
            days.Add(new AdminReferralDayDto
            {
                Date = d,
                Codes = codes.GetValueOrDefault(d),
                Invited = invited.GetValueOrDefault(d),
                Activated = activated.GetValueOrDefault(d),
                FirstPayments = firstPayments.GetValueOrDefault(d),
                PremiumGrants = grants.GetValueOrDefault(d)
            });
        }

        return days;
    }

    #endregion

    #region Taklif qiluvchilar reytingi

    /// <summary>
    /// Davrda do'st olib kelgan userlar reytingi. <paramref name="sort"/>: <c>invited</c> (default),
    /// <c>activated</c>, <c>paid</c>, <c>revenue</c>, <c>recent</c>. <paramref name="search"/> — ism, email,
    /// telefon yoki user id; <c>rank</c> umumiy reytingdagi o'rin bo'lib qoladi.
    /// </summary>
    public async Task<Wrapper> GetReferrers(DateTime? from, DateTime? to, string? search, string? sort,
        DataQueryRequest q)
    {
        var (start, end) = await Period(from, to);

        var refs = await LoadReferrals(start, end);
        var orders = await LoadOrders(refs.Select(x => x.ReferredUserId));

        var codes = await dbContext.ReferralCodes
            .AsNoTracking()
            .Where(x => x.CreatedAt >= start && x.CreatedAt <= end)
            .GroupBy(x => x.UserId)
            .Select(g => new { UserId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.UserId, x => x.Count);

        var groups = refs
            .GroupBy(x => x.ReferrerId)
            .Select(g =>
            {
                var paid = g.Select(r => OrdersAfter(orders, r)).Where(o => o.Count > 0).ToList();
                return new
                {
                    UserId = g.Key,
                    Invited = g.Count(),
                    Activated = g.Count(x => x.QualifiedAt != null),
                    Paid = paid.Count,
                    Revenue = paid.Sum(o => o.Sum(x => x.Amount)),
                    First = g.Min(x => x.CreatedAt),
                    Last = g.Max(x => x.CreatedAt)
                };
            });

        var sorted = (sort?.ToLowerInvariant() switch
            {
                "activated" => groups.OrderByDescending(x => x.Activated).ThenByDescending(x => x.Invited),
                "paid" => groups.OrderByDescending(x => x.Paid).ThenByDescending(x => x.Revenue),
                "revenue" => groups.OrderByDescending(x => x.Revenue).ThenByDescending(x => x.Paid),
                "recent" => groups.OrderByDescending(x => x.Last),
                _ => groups.OrderByDescending(x => x.Invited).ThenByDescending(x => x.Activated)
            })
            .ThenByDescending(x => x.Last)
            .ThenBy(x => x.UserId)
            .Select((x, i) => (Rank: i + 1, Row: x))
            .ToList();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            var pattern = $"%{term}%";
            var id = long.TryParse(term, out var parsed) ? parsed : 0;
            var ids = sorted.Select(x => x.Row.UserId).ToList();

            var matched = (await dbContext.Users
                    .IgnoreQueryFilters()
                    .Where(x => ids.Contains(x.Id))
                    .Where(x => x.Id == id ||
                                EF.Functions.ILike(x.Name, pattern) ||
                                EF.Functions.ILike(x.Email!, pattern) ||
                                EF.Functions.ILike(x.Phone!, pattern))
                    .Select(x => x.Id)
                    .ToListAsync())
                .ToHashSet();

            sorted = sorted.Where(x => matched.Contains(x.Row.UserId)).ToList();
        }

        var total = sorted.Count;
        var page = sorted.Skip(Math.Max(q.Skip, 0)).Take(q.Take > 0 ? q.Take : 20).ToList();
        var pageIds = page.Select(x => x.Row.UserId).ToList();

        var users = await LoadUsers(pageIds);
        var photos = await LoadPhotos(pageIds);
        var premium = await LoadPremium(pageIds);

        var allTime = await dbContext.Referrals
            .Where(x => pageIds.Contains(x.ReferrerId))
            .GroupBy(x => x.ReferrerId)
            .Select(g => new
            {
                UserId = g.Key,
                Invited = g.Count(),
                Activated = g.Count(x => x.QualifiedAt != null)
            })
            .ToDictionaryAsync(x => x.UserId);

        var grants = await dbContext.ReferralPremiumGrants
            .Where(x => pageIds.Contains(x.ReferrerId))
            .GroupBy(x => x.ReferrerId)
            .Select(g => new { UserId = g.Key, Count = g.Count(), Days = g.Sum(x => x.Days) })
            .ToDictionaryAsync(x => x.UserId);

        var result = page.Select(p =>
        {
            var x = p.Row;
            var user = users.GetValueOrDefault(x.UserId);
            var life = allTime.GetValueOrDefault(x.UserId);
            var grant = grants.GetValueOrDefault(x.UserId);
            var lifeActive = life?.Activated ?? 0;

            return new AdminReferrerRowDto
            {
                Rank = p.Rank,
                UserId = x.UserId,
                Name = user?.Name,
                Email = user?.Email,
                Phone = user?.Phone,
                Photo = photos.GetValueOrDefault(x.UserId),
                RegisteredAt = user?.CreatedAt,
                IsDeleted = user?.IsDeleted ?? true,
                IsPremium = premium.Contains(x.UserId),
                Invited = x.Invited,
                Activated = x.Activated,
                Pending = x.Invited - x.Activated,
                Paid = x.Paid,
                Revenue = Som(x.Revenue),
                CodesCreated = codes.GetValueOrDefault(x.UserId),
                ActivationRate = Rate(x.Activated, x.Invited),
                FirstInviteAt = x.First,
                LastInviteAt = x.Last,
                TotalInvited = life?.Invited ?? 0,
                TotalActivated = lifeActive,
                PremiumGrants = grant?.Count ?? 0,
                PremiumDays = grant?.Days ?? 0,
                FriendsLeft = Goal - lifeActive % Goal
            };
        }).ToList();

        return (result, total);
    }

    #endregion

    #region Takliflar ro'yxati

    /// <summary>
    /// Kim kimni taklif qilgani (yangidan eskiga). <paramref name="status"/>: <c>Joined</c> (hali faol emas),
    /// <c>Active</c> (faol), <c>Paid</c> (kod kiritgandan keyin to'lov qilgan). <paramref name="search"/> —
    /// taklif qiluvchi yoki do'stning ismi/email/telefoni/id si yoki kod.
    /// </summary>
    public async Task<Wrapper> GetReferrals(DateTime? from, DateTime? to, string? status, long? referrerId,
        string? search, DataQueryRequest q)
    {
        // Bitta taklif qiluvchining do'stlari — butun vaqt (davr berilmasa).
        DateTime? start = null, end = null;
        if (!referrerId.HasValue || from is not null || to is not null)
            (start, end) = await Period(from, to);

        var query = dbContext.Referrals
            .AsNoTracking()
            .Where(x => start == null || x.CreatedAt >= start)
            .Where(x => end == null || x.CreatedAt <= end)
            .Where(x => referrerId == null || x.ReferrerId == referrerId);

        switch (status?.ToLowerInvariant())
        {
            case "joined":
                query = query.Where(x => x.QualifiedAt == null);
                break;
            case "active":
                query = query.Where(x => x.QualifiedAt != null);
                break;
            case "paid":
                query = query.Where(x => dbContext.Orders.Any(o =>
                    o.UserId == x.ReferredUserId && o.Status == EnumOrderStatus.Confirmed &&
                    o.CreatedAt >= x.CreatedAt));
                break;
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            var pattern = $"%{term}%";
            var id = long.TryParse(term, out var parsed) ? parsed : 0;

            query = query.Where(x =>
                EF.Functions.ILike(x.Code!, pattern) ||
                dbContext.Users.Any(u =>
                    (u.Id == x.ReferrerId || u.Id == x.ReferredUserId) &&
                    (u.Id == id ||
                     EF.Functions.ILike(u.Name, pattern) ||
                     EF.Functions.ILike(u.Email!, pattern) ||
                     EF.Functions.ILike(u.Phone!, pattern))));
        }

        var total = await query.CountAsync();
        var rows = await query
            .OrderByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.Id)
            .Page(q)
            .Select(x => new
            {
                Ref = new RefRow(x.Id, x.ReferrerId, x.ReferredUserId, x.CreatedAt, x.QualifiedAt),
                x.Code,
                x.DiscountUsedAt
            })
            .ToListAsync();

        var userIds = rows.SelectMany(x => new[] { x.Ref.ReferrerId, x.Ref.ReferredUserId }).ToList();
        var users = await LoadUsers(userIds);
        var referredIds = rows.Select(x => x.Ref.ReferredUserId).ToList();
        var photos = await LoadPhotos(referredIds);
        var premium = await LoadPremium(referredIds);
        var orders = await LoadOrders(referredIds);

        var result = rows.Select(x =>
        {
            var referrer = users.GetValueOrDefault(x.Ref.ReferrerId);
            var referred = users.GetValueOrDefault(x.Ref.ReferredUserId);
            var paid = OrdersAfter(orders, x.Ref);

            return new AdminReferralRowDto
            {
                Id = x.Ref.Id,
                Code = x.Code,
                CreatedAt = x.Ref.CreatedAt,
                ActivatedAt = x.Ref.QualifiedAt,
                Status = paid.Count > 0 ? "Paid" : x.Ref.QualifiedAt != null ? "Active" : "Joined",
                ReferrerId = x.Ref.ReferrerId,
                ReferrerName = referrer?.Name,
                ReferrerContact = Contact(referrer),
                ReferredUserId = x.Ref.ReferredUserId,
                ReferredName = referred?.Name,
                ReferredContact = Contact(referred),
                ReferredPhoto = photos.GetValueOrDefault(x.Ref.ReferredUserId),
                ReferredRegisteredAt = referred?.CreatedAt,
                ReferredDeleted = referred?.IsDeleted ?? true,
                DaysAfterSignup = referred is null
                    ? null
                    : Math.Max(0, (x.Ref.CreatedAt.Date - referred.CreatedAt.Date).Days),
                FirstPaymentAt = paid.FirstOrDefault()?.CreatedAt,
                Orders = paid.Count,
                Revenue = Som(paid.Sum(o => o.Amount)),
                DiscountUsedAt = x.DiscountUsedAt,
                IsPremium = premium.Contains(x.Ref.ReferredUserId)
            };
        }).ToList();

        return (result, total);
    }

    #endregion

    #region Taklif qiluvchi kartasi

    public async Task<AdminReferrerDetailDto> GetReferrer(long userId)
    {
        var user = (await LoadUsers([userId])).GetValueOrDefault(userId)
                   ?? throw new UserNotFoundException();

        var refs = await LoadReferrals(null, null, userId);
        var orders = await LoadOrders(refs.Select(x => x.ReferredUserId));
        var paid = refs.Select(r => OrdersAfter(orders, r)).Where(o => o.Count > 0).ToList();
        var activated = refs.Count(x => x.QualifiedAt != null);

        var codes = await dbContext.ReferralCodes
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.Id)
            .Select(x => new { x.Code, x.CreatedAt })
            .ToListAsync();

        var grants = await dbContext.ReferralPremiumGrants
            .AsNoTracking()
            .Where(x => x.ReferrerId == userId)
            .OrderBy(x => x.Milestone)
            .Select(x => new AdminReferrerGrantDto { Milestone = x.Milestone, Days = x.Days, CreatedAt = x.CreatedAt })
            .ToListAsync();

        var referredBy = await dbContext.Referrals
            .AsNoTracking()
            .Where(x => x.ReferredUserId == userId)
            .Select(x => new { x.ReferrerId, x.CreatedAt })
            .FirstOrDefaultAsync();
        var referredByUser = referredBy is null
            ? null
            : (await LoadUsers([referredBy.ReferrerId])).GetValueOrDefault(referredBy.ReferrerId);

        var now = DateTime.Now;
        var subscription = await dbContext.Subscriptions
            .AsNoTracking()
            .Where(x => x.UserId == userId && x.IsActive && x.EndsAt > now)
            .Select(x => (DateTime?)x.EndsAt)
            .FirstOrDefaultAsync();

        var progress = activated % Goal;

        return new AdminReferrerDetailDto
        {
            UserId = user.Id,
            Name = user.Name,
            Email = user.Email,
            Phone = user.Phone,
            Photo = (await LoadPhotos([userId])).GetValueOrDefault(userId),
            RegisteredAt = user.CreatedAt,
            IsPremium = subscription.HasValue,
            PremiumEndsAt = subscription,
            Invited = refs.Count,
            Activated = activated,
            Pending = refs.Count - activated,
            Paid = paid.Count,
            Revenue = Som(paid.Sum(o => o.Sum(x => x.Amount))),
            CodesCreated = codes.Count,
            LatestCode = codes.FirstOrDefault()?.Code,
            LastCodeAt = codes.FirstOrDefault()?.CreatedAt,
            FirstInviteAt = refs.Count > 0 ? refs.Min(x => x.CreatedAt) : null,
            LastInviteAt = refs.Count > 0 ? refs.Max(x => x.CreatedAt) : null,
            FriendsGoal = Goal,
            ProgressFriends = progress,
            FriendsLeft = Goal - progress,
            Grants = grants,
            ReferredById = referredBy?.ReferrerId,
            ReferredByName = referredByUser?.Name,
            ReferredAt = referredBy?.CreatedAt,
            Days = await BuildDays(now.Date.AddDays(1 - DetailDays), now, userId)
        };
    }

    #endregion
}
