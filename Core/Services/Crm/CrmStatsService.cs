using BRB.Core.EF.Attributes;
using Core.Brokers.DbContext;
using Core.Entities.Billing;
using Core.Entities.Billing.Enum;
using Core.Entities.Crm.Enum;
using Core.Services.Crm.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Core.Services.Crm;

[Injectable]
public class CrmStatsService(AppDbContext context)
{
    /// <summary>Card payments = Click/Payme; platform = in-app purchase (RevenueCat/IAP).</summary>
    public static readonly EnumPaymentProviders[] CardProviders = [EnumPaymentProviders.Click, EnumPaymentProviders.Payme];

    /// <summary>Order amounts are stored in tiyin; the dashboard shows so'm (1 so'm = 100 tiyin).</summary>
    private const int TiyinPerSom = 100;

    /// <summary>Real sales aggregated from Confirmed orders — the single source of truth.</summary>
    public record SalesAgg
    {
        public int Sales { get; init; }
        public long Revenue { get; init; }        // so'm
        public int CardSales { get; init; }
        public int PlatformSales { get; init; }
        public int PromoSales { get; init; }
        public long PurchaseRevenue { get; init; } // so'm (card + platform, no coupon)
        public long PromoRevenue { get; init; }    // so'm (coupon used)
    }

    public static (DateTime from, DateTime to) ResolveRange(EnumStatsPeriod period)
    {
        var now = DateTime.Now;
        var to = now;
        var from = period switch
        {
            EnumStatsPeriod.Day => now.Date,
            EnumStatsPeriod.Week => now.Date.AddDays(-(int)now.DayOfWeek),
            EnumStatsPeriod.Month => new DateTime(now.Year, now.Month, 1),
            _ => now.Date
        };
        return (from, to);
    }

    /// <summary>
    /// Aggregates real sales straight from Confirmed <see cref="Order"/> rows (the same source the
    /// "Savdo" dashboard uses), converting tiyin → so'm. When <paramref name="operatorId"/> is set the
    /// result is scoped to orders placed by users whose lead is Won and owned by that operator, so an
    /// operator's revenue reflects deals they actually closed. Date bounds are [from, to).
    /// </summary>
    public async Task<SalesAgg> AggregateSalesAsync(long? operatorId, DateTime? from, DateTime? to)
    {
        var orders = context.Orders.Where(o => o.Status == EnumOrderStatus.Confirmed);
        if (from.HasValue) orders = orders.Where(o => o.CreatedAt >= from.Value);
        if (to.HasValue) orders = orders.Where(o => o.CreatedAt < to.Value);

        if (operatorId.HasValue)
        {
            var opId = operatorId.Value;
            orders = orders.Where(o => context.Leads.Any(l =>
                l.UserId == o.UserId && l.OperatorId == opId && l.Status == EnumLeadStatus.Won));
        }

        var rows = await orders.Select(o => new { o.Amount, o.Provider, o.CouponId }).ToListAsync();

        var promo = rows.Where(r => r.CouponId != null).ToList();
        var purchase = rows.Where(r => r.CouponId == null).ToList();

        return new SalesAgg
        {
            Sales = rows.Count,
            Revenue = rows.Sum(r => r.Amount) / TiyinPerSom,
            PromoSales = promo.Count,
            CardSales = purchase.Count(r => CardProviders.Contains(r.Provider)),
            PlatformSales = purchase.Count(r => r.Provider == EnumPaymentProviders.Iap),
            PurchaseRevenue = purchase.Sum(r => r.Amount) / TiyinPerSom,
            PromoRevenue = promo.Sum(r => r.Amount) / TiyinPerSom
        };
    }

    public async Task<OperatorDashboardDto> GetOperatorDashboardAsync(long operatorId)
    {
        var todayStart = DateTime.Now.Date;
        var now = DateTime.Now;

        var leads = context.Leads.Where(l => l.OperatorId == operatorId);

        var myLeads = await leads.CountAsync();
        var activeLeads = await leads.CountAsync(l => l.Status != EnumLeadStatus.Won && l.Status != EnumLeadStatus.Lost);
        var hotLeads = await leads.CountAsync(l =>
            (l.Temperature == EnumLeadTemperature.Hot || l.Temperature == EnumLeadTemperature.VeryHot)
            && l.Status != EnumLeadStatus.Won && l.Status != EnumLeadStatus.Lost);

        var todayCalls = await context.LeadActivities.CountAsync(a =>
            a.ActorId == operatorId && a.Type == EnumLeadActivityType.Contacted && a.CreatedAt >= todayStart);

        var followUps = context.FollowUps.Where(f => f.OperatorId == operatorId && !f.IsDone);
        var todayFollowUps = await followUps.CountAsync(f => f.DueAt < todayStart.AddDays(1));
        var overdueFollowUps = await followUps.CountAsync(f => f.DueAt < now);

        // Sotuv/tushum — haqiqiy tasdiqlangan buyurtmalardan (Lead.WonAmount emas).
        var today = await AggregateSalesAsync(operatorId, todayStart, null);
        var allTime = await AggregateSalesAsync(operatorId, null, null);
        var conversion = myLeads == 0 ? 0 : Math.Round(allTime.Sales * 100.0 / myLeads, 1);

        return new OperatorDashboardDto
        {
            MyLeads = myLeads,
            ActiveLeads = activeLeads,
            HotLeads = hotLeads,
            TodayCalls = todayCalls,
            TodayFollowUps = todayFollowUps,
            OverdueFollowUps = overdueFollowUps,
            TodaySales = today.Sales,
            TodayRevenue = today.Revenue,
            ConversionRate = conversion
        };
    }

    public async Task<OperatorStatsDto> GetOperatorStatsAsync(long operatorId, EnumStatsPeriod period)
    {
        var (from, to) = ResolveRange(period);

        // "Worked" = leads the operator actively touched (contacted, status change, note,
        // follow-up, won/lost). Auto-assignment also stamps ActorId, so exclude it — otherwise
        // a lead the operator never engaged with would inflate the count.
        var leadsWorked = await context.LeadActivities
            .Where(a => a.ActorId == operatorId && a.Type != EnumLeadActivityType.Assigned
                                                && a.CreatedAt >= from && a.CreatedAt <= to)
            .Select(a => a.LeadId)
            .Distinct()
            .CountAsync();

        var calls = await context.LeadActivities.CountAsync(a =>
            a.ActorId == operatorId && a.Type == EnumLeadActivityType.Contacted
            && a.CreatedAt >= from && a.CreatedAt <= to);

        // Sotuv/tushum va to'lov turlari — haqiqiy tasdiqlangan buyurtmalardan.
        var agg = await AggregateSalesAsync(operatorId, from, to);
        var conversion = leadsWorked == 0 ? 0 : Math.Round(agg.Sales * 100.0 / leadsWorked, 1);

        return new OperatorStatsDto
        {
            Period = period,
            LeadsWorked = leadsWorked,
            Calls = calls,
            Sales = agg.Sales,
            Revenue = agg.Revenue,
            ConversionRate = conversion,
            CardSales = agg.CardSales,
            PlatformSales = agg.PlatformSales,
            PromoSales = agg.PromoSales
        };
    }
}
