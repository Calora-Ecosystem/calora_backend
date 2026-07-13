using BRB.Core.EF.Attributes;
using Core.Brokers.DbContext;
using Core.Entities.Billing.Enum;
using Core.Entities.Crm.Enum;
using Core.Services.Crm.Contracts;
using Core.Services.Crm.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Core.Services.Crm;

[Injectable]
public class SalesAnalyticsService(AppDbContext context, CrmStatsService statsService)
{
    // Roles is a jsonb List<string>; Postgres containment, since EF can't translate LINQ Contains on it.
    private IQueryable<Core.Entities.Auth.User> OperatorUsers() =>
        context.Users.FromSqlInterpolated($"SELECT * FROM users WHERE roles @> '[\"Operator\"]'::jsonb");

    public async Task<SalesOverviewDto> GetOverviewAsync()
    {
        var todayStart = DateTime.Now.Date;

        var totalLeads = await context.Leads.CountAsync();
        var todayLeads = await context.Leads.CountAsync(l => l.CreatedAt >= todayStart);
        var activeLeads = await context.Leads.CountAsync(l => l.Status != EnumLeadStatus.Won && l.Status != EnumLeadStatus.Lost);
        var hotLeads = await context.Leads.CountAsync(l =>
            (l.Temperature == EnumLeadTemperature.Hot || l.Temperature == EnumLeadTemperature.VeryHot)
            && l.Status != EnumLeadStatus.Won && l.Status != EnumLeadStatus.Lost);

        var todayCalls = await context.LeadActivities.CountAsync(a =>
            a.Type == EnumLeadActivityType.Contacted && a.CreatedAt >= todayStart);

        // Sotuv/tushum — haqiqiy tasdiqlangan buyurtmalardan (Savdo bo'limi bilan bir manba).
        var today = await statsService.AggregateSalesAsync(null, todayStart, null);
        var allTime = await statsService.AggregateSalesAsync(null, null, null);
        var conversion = totalLeads == 0 ? 0 : Math.Round(allTime.Sales * 100.0 / totalLeads, 1);

        var operatorsCount = await OperatorUsers().CountAsync();

        return new SalesOverviewDto
        {
            TotalLeads = totalLeads,
            TodayLeads = todayLeads,
            ActiveLeads = activeLeads,
            HotLeads = hotLeads,
            TodayCalls = todayCalls,
            TodaySales = today.Sales,
            TodayRevenue = today.Revenue,
            ConversionRate = conversion,
            OperatorsCount = operatorsCount
        };
    }

    public async Task<IEnumerable<FunnelStageDto>> GetFunnelAsync()
    {
        var counts = await context.Leads
            .GroupBy(l => l.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Status, x => x.Count);

        // Pipeline order; Lost is reported separately at the end.
        EnumLeadStatus[] order =
        [
            EnumLeadStatus.New, EnumLeadStatus.Contacted, EnumLeadStatus.FollowUp,
            EnumLeadStatus.Interested, EnumLeadStatus.PaymentInProgress, EnumLeadStatus.Won
        ];

        var result = new List<FunnelStageDto>();
        int? previous = null;
        foreach (var status in order)
        {
            var count = counts.GetValueOrDefault(status, 0);
            var conv = previous is null or 0 ? 100.0 : Math.Round(count * 100.0 / previous.Value, 1);
            result.Add(new FunnelStageDto { Status = status, Count = count, ConversionFromPrevious = conv });
            previous = count;
        }

        result.Add(new FunnelStageDto
        {
            Status = EnumLeadStatus.Lost,
            Count = counts.GetValueOrDefault(EnumLeadStatus.Lost, 0),
            ConversionFromPrevious = 0
        });

        return result;
    }

    public async Task<IEnumerable<OperatorLeaderboardRowDto>> GetLeaderboardAsync(EnumStatsPeriod period)
    {
        var (from, to) = CrmStatsService.ResolveRange(period);

        var operators = await OperatorUsers()
            .Select(u => new { u.Id, u.Name })
            .ToListAsync();

        var rows = new List<OperatorLeaderboardRowDto>();
        foreach (var op in operators)
        {
            var leads = await context.Leads.CountAsync(l => l.OperatorId == op.Id);

            var calls = await context.LeadActivities.CountAsync(a =>
                a.ActorId == op.Id && a.Type == EnumLeadActivityType.Contacted
                && a.CreatedAt >= from && a.CreatedAt <= to);

            var agg = await statsService.AggregateSalesAsync(op.Id, from, to);
            var conversion = leads == 0 ? 0 : Math.Round(agg.Sales * 100.0 / leads, 1);

            rows.Add(new OperatorLeaderboardRowDto
            {
                OperatorId = op.Id,
                OperatorName = op.Name,
                Leads = leads,
                Calls = calls,
                Sales = agg.Sales,
                Revenue = agg.Revenue,
                ConversionRate = conversion
            });
        }

        return rows.OrderByDescending(r => r.Sales).ThenByDescending(r => r.Revenue);
    }

    public async Task<IEnumerable<RevenuePointDto>> GetRevenueAsync(EnumStatsPeriod period)
    {
        var now = DateTime.Now;
        var points = new List<RevenuePointDto>();

        // Day -> last 14 days; Week -> last 8 weeks; Month -> last 12 months.
        switch (period)
        {
            case EnumStatsPeriod.Week:
                for (var i = 7; i >= 0; i--)
                {
                    var start = now.Date.AddDays(-(int)now.DayOfWeek).AddDays(-7 * i);
                    var end = start.AddDays(7);
                    points.Add(await RevenuePoint($"{start:dd.MM}", start, start, end));
                }
                break;

            case EnumStatsPeriod.Month:
                for (var i = 11; i >= 0; i--)
                {
                    var start = new DateTime(now.Year, now.Month, 1).AddMonths(-i);
                    var end = start.AddMonths(1);
                    points.Add(await RevenuePoint($"{start:MM.yyyy}", start, start, end));
                }
                break;

            default: // Day
                for (var i = 13; i >= 0; i--)
                {
                    var start = now.Date.AddDays(-i);
                    var end = start.AddDays(1);
                    points.Add(await RevenuePoint($"{start:dd.MM}", start, start, end));
                }
                break;
        }

        return points;
    }

    private async Task<RevenuePointDto> RevenuePoint(string label, DateTime date, DateTime from, DateTime to)
    {
        var agg = await statsService.AggregateSalesAsync(null, from, to);
        return new RevenuePointDto
        {
            Label = label,
            Date = date,
            Sales = agg.Sales,
            Revenue = agg.Revenue
        };
    }

    public async Task<OperatorStatsDto> GetOperatorStatsAsync(long operatorId, EnumStatsPeriod period)
    {
        if (!await OperatorUsers().AnyAsync(u => u.Id == operatorId))
            throw new LeadNotFoundException();

        return await statsService.GetOperatorStatsAsync(operatorId, period);
    }

    public async Task<IEnumerable<OperatorLeaderboardRowDto>> GetOperatorsAsync()
    {
        // All-time operator summary for the operators directory.
        var operators = await OperatorUsers()
            .Select(u => new { u.Id, u.Name })
            .ToListAsync();

        var rows = new List<OperatorLeaderboardRowDto>();
        foreach (var op in operators)
        {
            var leads = await context.Leads.CountAsync(l => l.OperatorId == op.Id);
            var agg = await statsService.AggregateSalesAsync(op.Id, null, null);
            var calls = await context.LeadActivities.CountAsync(a => a.ActorId == op.Id && a.Type == EnumLeadActivityType.Contacted);
            var conversion = leads == 0 ? 0 : Math.Round(agg.Sales * 100.0 / leads, 1);

            rows.Add(new OperatorLeaderboardRowDto
            {
                OperatorId = op.Id,
                OperatorName = op.Name,
                Leads = leads,
                Calls = calls,
                Sales = agg.Sales,
                Revenue = agg.Revenue,
                ConversionRate = conversion
            });
        }

        return rows.OrderByDescending(r => r.Sales);
    }

    /// <summary>
    /// Premium acquisition breakdown across won deals: platform purchase (card = Click/Payme,
    /// platform = IAP) vs promo-code (coupon was used). Period optional (null = all time).
    /// </summary>
    public async Task<PremiumBreakdownDto> GetPremiumBreakdownAsync(EnumStatsPeriod? period)
    {
        DateTime? from = null, to = null;
        if (period.HasValue)
            (from, to) = CrmStatsService.ResolveRange(period.Value);

        // Haqiqiy tasdiqlangan buyurtmalardan (tiyin → so'm, /100 statsService ichida).
        var agg = await statsService.AggregateSalesAsync(null, from, to);

        return new PremiumBreakdownDto
        {
            Total = agg.Sales,
            ViaPromoCode = agg.PromoSales,
            ViaPurchase = agg.CardSales + agg.PlatformSales,
            Card = agg.CardSales,
            Platform = agg.PlatformSales,
            PromoRevenue = agg.PromoRevenue,
            PurchaseRevenue = agg.PurchaseRevenue
        };
    }

    /// <summary>Recent premium grants obtained through a promo-code (for the promo-code section).</summary>
    public async Task<IEnumerable<PromoRedemptionDto>> GetPromoRedemptionsAsync()
    {
        return await context.Leads
            .Where(l => l.Status == EnumLeadStatus.Won && l.CouponId != null)
            .OrderByDescending(l => l.WonAt)
            .Select(l => new PromoRedemptionDto
            {
                LeadId = l.Id,
                UserName = l.User.Name,
                UserPhone = l.User.Phone,
                PromoCode = l.PromoCode,
                Amount = l.WonAmount != null ? l.WonAmount / 100 : null, // tiyin → so'm
                OperatorName = l.Operator != null ? l.Operator.Name : null,
                WonAt = l.WonAt
            })
            .Take(100)
            .ToListAsync();
    }
}
