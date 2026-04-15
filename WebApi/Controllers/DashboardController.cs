using BRB.Core.Common.Models;
using Core.Attributes;
using Core.Enums;
using Core.Services.Dashboard;
using Core.Services.Dashboard.Contracts;
using Microsoft.AspNetCore.Mvc;
using ResultWrapper.Library;
using WebCore.Controller;

namespace WebApi.Controllers;

[ApiController]
[Route("dashboard")]
[RoleAuthorize(EnumRole.SuperAdmin)]
public class DashboardController(DashboardService dashboardService) : AuthorizedController
{
    [HttpGet("summary")]
    [ProducesResponseType<WrapperGeneric<GetOverallSummaryDto>>(200)]
    public async Task<Wrapper> GetOverallSummary() =>
        (await dashboardService.GetOverallSummary(), 200);

    [HttpGet("sales/summary")]
    [ProducesResponseType<WrapperGeneric<Dictionary<int, double>>>(200)]
    public async Task<Wrapper> SalesSummary() =>
        (await dashboardService.GetSalesMonthlySummary(), 200);

    [HttpGet("orders/subscriptions")]
    [ProducesResponseType<WrapperGeneric<IEnumerable<GetSubscriptionOrdersDto>>>(200)]
    public async Task<Wrapper> GetAllOrders([FromQuery] DataQueryRequest query) =>
        await dashboardService.GetSubscriptionOrders(query);
}