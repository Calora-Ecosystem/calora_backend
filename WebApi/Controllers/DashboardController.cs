using BRB.Core.Common.Models;
using Core.Attributes;
using Core.Enums;
using Core.Services.Billing;
using Core.Services.Billing.Contracts;
using Core.Services.Dashboard;
using Core.Services.Dashboard.Contracts;
using Microsoft.AspNetCore.Mvc;
using ResultWrapper.Library;
using WebCore.Controller;

namespace WebApi.Controllers;

[ApiController]
[Route("dashboard")]
[RoleAuthorize(EnumRole.SuperAdmin)]
public class DashboardController(DashboardService dashboardService, SubscriptionService subscriptionService)
    : AuthorizedController
{
    [HttpGet("summary")]
    [ProducesResponseType<WrapperGeneric<GetOverallSummaryDto>>(200)]
    public async Task<Wrapper> GetOverallSummary() =>
        (await dashboardService.GetOverallSummary(), 200);

    [HttpGet("sales/summary")]
    [ProducesResponseType<WrapperGeneric<Dictionary<int, double>>>(200)]
    public async Task<Wrapper> SalesSummary() =>
        (await dashboardService.GetSalesMonthlySummary(), 200);

    [HttpGet("users/statistics")]
    [ProducesResponseType<WrapperGeneric<GetUserStatisticsDto>>(200)]
    public async Task<Wrapper> GetUserStatistics() =>
        (await dashboardService.GetUserStatistics(), 200);

    [HttpGet("users/statistics/range")]
    [ProducesResponseType<WrapperGeneric<GetUserStatisticsRangeDto>>(200)]
    public async Task<Wrapper> GetUserStatisticsRange([FromQuery] DateTime from, [FromQuery] DateTime to) =>
        (await dashboardService.GetUserStatisticsRange(from, to), 200);

    [HttpGet("users/audience")]
    [ProducesResponseType<WrapperGeneric<GetAudienceAnalyticsDto>>(200)]
    public async Task<Wrapper> GetAudienceAnalytics() =>
        (await dashboardService.GetAudienceAnalytics(), 200);

    [HttpGet("users/{userId:long:min(1)}/detail")]
    [ProducesResponseType<WrapperGeneric<GetUserDetailDto>>(200)]
    public async Task<Wrapper> GetUserDetail(long userId) =>
        (await dashboardService.GetUserDetail(userId), 200);

    [HttpGet("orders/subscriptions")]
    [ProducesResponseType<WrapperGeneric<IEnumerable<GetSubscriptionOrdersDto>>>(200)]
    public async Task<Wrapper> GetAllOrders([FromQuery] DataQueryRequest query) =>
        await dashboardService.GetSubscriptionOrders(query);

    #region Subscriptions (admin-managed)

    [HttpGet("subscriptions/{userId:long:min(1)}")]
    [ProducesResponseType<WrapperGeneric<GetSubscriptionDto>>(200)]
    public async Task<Wrapper> GetUserSubscription(long userId) =>
        (await subscriptionService.GetByUserId(userId), 200);

    [HttpPost("subscriptions")]
    [ProducesResponseType<WrapperGeneric<GetSubscriptionDto>>(200)]
    public async Task<Wrapper> CreateOrUpdateSubscription([FromBody] CreateOrUpdateSubscriptionDto dto) =>
        (await subscriptionService.CreateOrUpdate(dto), 200);

    [HttpDelete("subscriptions/{userId:long:min(1)}")]
    public async Task<Wrapper> DeleteSubscription(long userId)
    {
        await subscriptionService.Delete(userId);
        return 200;
    }

    #endregion
}