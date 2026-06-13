using Core.Attributes;
using Core.Enums;
using Core.Services.Crm;
using Core.Services.Crm.Contracts;
using Microsoft.AspNetCore.Mvc;
using ResultWrapper.Library;
using WebCore.Controller;

namespace WebApi.Controllers;

[ApiController]
[Route("crm/analytics")]
[RoleAuthorize(EnumRole.HeadOfSales, EnumRole.SuperAdmin)]
public class SalesController(SalesAnalyticsService analyticsService, CrmOperatorService operatorService) : AuthorizedController
{
    [HttpPost("/crm/operators")]
    [ProducesResponseType<WrapperGeneric<long>>(200)]
    public async Task<Wrapper> CreateOperator([FromBody] CreateOperatorDto dto) =>
        (await operatorService.CreateOperatorAsync(dto), 200);

    [HttpDelete("/crm/operators/{operatorId:long:min(1)}")]
    public async Task<Wrapper> DeleteOperator(long operatorId)
    {
        await operatorService.DeleteOperatorAsync(operatorId);
        return 200;
    }

    [HttpGet("overview")]
    [ProducesResponseType<WrapperGeneric<SalesOverviewDto>>(200)]
    public async Task<Wrapper> GetOverview() =>
        (await analyticsService.GetOverviewAsync(), 200);

    [HttpGet("funnel")]
    [ProducesResponseType<WrapperGeneric<IEnumerable<FunnelStageDto>>>(200)]
    public async Task<Wrapper> GetFunnel() =>
        (await analyticsService.GetFunnelAsync(), 200);

    [HttpGet("leaderboard")]
    [ProducesResponseType<WrapperGeneric<IEnumerable<OperatorLeaderboardRowDto>>>(200)]
    public async Task<Wrapper> GetLeaderboard([FromQuery] EnumStatsPeriod period = EnumStatsPeriod.Month) =>
        (await analyticsService.GetLeaderboardAsync(period), 200);

    [HttpGet("revenue")]
    [ProducesResponseType<WrapperGeneric<IEnumerable<RevenuePointDto>>>(200)]
    public async Task<Wrapper> GetRevenue([FromQuery] EnumStatsPeriod period = EnumStatsPeriod.Day) =>
        (await analyticsService.GetRevenueAsync(period), 200);

    [HttpGet("premium")]
    [ProducesResponseType<WrapperGeneric<PremiumBreakdownDto>>(200)]
    public async Task<Wrapper> GetPremiumBreakdown([FromQuery] EnumStatsPeriod? period) =>
        (await analyticsService.GetPremiumBreakdownAsync(period), 200);

    [HttpGet("promo-redemptions")]
    [ProducesResponseType<WrapperGeneric<IEnumerable<PromoRedemptionDto>>>(200)]
    public async Task<Wrapper> GetPromoRedemptions() =>
        (await analyticsService.GetPromoRedemptionsAsync(), 200);

    [HttpGet("/crm/operators")]
    [ProducesResponseType<WrapperGeneric<IEnumerable<OperatorLeaderboardRowDto>>>(200)]
    public async Task<Wrapper> GetOperators() =>
        (await analyticsService.GetOperatorsAsync(), 200);

    [HttpGet("/crm/operators/{operatorId:long:min(1)}/stats")]
    [ProducesResponseType<WrapperGeneric<OperatorStatsDto>>(200)]
    public async Task<Wrapper> GetOperatorStats(long operatorId, [FromQuery] EnumStatsPeriod period = EnumStatsPeriod.Month) =>
        (await analyticsService.GetOperatorStatsAsync(operatorId, period), 200);
}
