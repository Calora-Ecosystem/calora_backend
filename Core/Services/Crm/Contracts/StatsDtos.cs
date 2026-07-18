using Core.Entities.Crm.Enum;

namespace Core.Services.Crm.Contracts;

/// <summary>Operator self dashboard (today figures + active workload).</summary>
public record OperatorDashboardDto
{
    public int MyLeads { get; set; }
    public int ActiveLeads { get; set; }
    public int HotLeads { get; set; }
    public int TodayCalls { get; set; }
    public int TodayFollowUps { get; set; }
    public int OverdueFollowUps { get; set; }
    public int TodaySales { get; set; }
    public long TodayRevenue { get; set; }
    public double ConversionRate { get; set; }
}

/// <summary>Operator metrics for a given period (day/week/month).</summary>
public record OperatorStatsDto
{
    public EnumStatsPeriod Period { get; set; }
    public int LeadsWorked { get; set; }
    public int Calls { get; set; }
    public int Sales { get; set; }
    public long Revenue { get; set; }
    public double ConversionRate { get; set; }
    public int CardSales { get; set; }
    public int PlatformSales { get; set; }
    public int PromoSales { get; set; }
}

public record FunnelStageDto
{
    public EnumLeadStatus Status { get; set; }
    public int Count { get; set; }
    public double ConversionFromPrevious { get; set; }
}

public record OperatorLeaderboardRowDto
{
    public long OperatorId { get; set; }
    public string OperatorName { get; set; } = null!;
    public int Leads { get; set; }

    /// <summary>Current active (non Won/Lost) leads — the operator's live workload.</summary>
    public int ActiveLeads { get; set; }
    public int Calls { get; set; }
    public int Sales { get; set; }
    public long Revenue { get; set; }
    public double ConversionRate { get; set; }
}

public record SalesOverviewDto
{
    public int TotalLeads { get; set; }
    public int TodayLeads { get; set; }
    public int ActiveLeads { get; set; }
    public int HotLeads { get; set; }
    public int TodayCalls { get; set; }
    public int TodaySales { get; set; }
    public long TodayRevenue { get; set; }
    public double ConversionRate { get; set; }
    public int OperatorsCount { get; set; }
}

public record RevenuePointDto
{
    public string Label { get; set; } = null!;
    public DateTime Date { get; set; }
    public long Revenue { get; set; }
    public int Sales { get; set; }
}

/// <summary>How users obtained premium: platform purchase (card/IAP) vs promo-code.</summary>
public record PremiumBreakdownDto
{
    public int Total { get; set; }
    public int ViaPurchase { get; set; }
    public int ViaPromoCode { get; set; }
    public int Card { get; set; }       // Click / Payme, no coupon
    public int Platform { get; set; }   // in-app purchase (IAP), no coupon
    public long PurchaseRevenue { get; set; }
    public long PromoRevenue { get; set; }
}

public record PromoRedemptionDto
{
    public long LeadId { get; set; }
    public string? UserName { get; set; }
    public string? UserPhone { get; set; }
    public string? PromoCode { get; set; }
    public long? Amount { get; set; }
    public string? OperatorName { get; set; }
    public DateTime? WonAt { get; set; }
}
