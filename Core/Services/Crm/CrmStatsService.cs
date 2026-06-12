using BRB.Core.EF.Attributes;
using Core.Brokers.DbContext;
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

        var todayWon = leads.Where(l => l.Status == EnumLeadStatus.Won && l.WonAt >= todayStart);
        var todaySales = await todayWon.CountAsync();
        var todayRevenue = await todayWon.SumAsync(l => (long?)l.WonAmount) ?? 0;

        var totalWon = await leads.CountAsync(l => l.Status == EnumLeadStatus.Won);
        var conversion = myLeads == 0 ? 0 : Math.Round(totalWon * 100.0 / myLeads, 1);

        return new OperatorDashboardDto
        {
            MyLeads = myLeads,
            ActiveLeads = activeLeads,
            HotLeads = hotLeads,
            TodayCalls = todayCalls,
            TodayFollowUps = todayFollowUps,
            OverdueFollowUps = overdueFollowUps,
            TodaySales = todaySales,
            TodayRevenue = todayRevenue,
            ConversionRate = conversion
        };
    }

    public async Task<OperatorStatsDto> GetOperatorStatsAsync(long operatorId, EnumStatsPeriod period)
    {
        var (from, to) = ResolveRange(period);

        var leadsWorked = await context.LeadActivities
            .Where(a => a.ActorId == operatorId && a.CreatedAt >= from && a.CreatedAt <= to)
            .Select(a => a.LeadId)
            .Distinct()
            .CountAsync();

        var calls = await context.LeadActivities.CountAsync(a =>
            a.ActorId == operatorId && a.Type == EnumLeadActivityType.Contacted
            && a.CreatedAt >= from && a.CreatedAt <= to);

        var won = context.Leads.Where(l => l.OperatorId == operatorId && l.Status == EnumLeadStatus.Won
                                                                      && l.WonAt >= from && l.WonAt <= to);

        var sales = await won.CountAsync();
        var revenue = await won.SumAsync(l => (long?)l.WonAmount) ?? 0;
        var cardSales = await won.CountAsync(l => l.PaymentProvider != null && CardProviders.Contains(l.PaymentProvider.Value));
        var platformSales = sales - cardSales;

        var conversion = leadsWorked == 0 ? 0 : Math.Round(sales * 100.0 / leadsWorked, 1);

        return new OperatorStatsDto
        {
            Period = period,
            LeadsWorked = leadsWorked,
            Calls = calls,
            Sales = sales,
            Revenue = revenue,
            ConversionRate = conversion,
            CardSales = cardSales,
            PlatformSales = platformSales
        };
    }
}
