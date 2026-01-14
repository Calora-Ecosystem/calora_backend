using BRB.Core.Common.Models;
using Core;
using Core.Attributes;
using Core.Enums;
using Core.Services.Billing;
using Core.Services.Billing.Click;
using Core.Services.Billing.Click.Contracts;
using Core.Services.Billing.Contracts;
using Core.Services.Billing.Payme;
using Core.Services.Billing.Payme.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ResultWrapper.Library;
using Serilog;
using WebCore.Controller;

namespace WebApi.Controllers;

[ApiController]
[Route("billing")]
public class BillingController(OrderService orderService, ClickService clickService, PaymeService paymeService)
    : AuthorizedController
{
    #region Orders

    [HttpGet("orders")]
    [RoleAuthorize(EnumRole.SuperAdmin)]
    [ProducesResponseType<WrapperGeneric<GetOrdersDto>>(200)]
    public async Task<Wrapper> GetAllOrders([FromQuery] DataQueryRequest query) =>
        await orderService.GetOrders(query);

    [HttpGet("orders/my")]
    [RoleAuthorize(EnumRole.User)]
    [ProducesResponseType<WrapperGeneric<GetOrdersDto>>(200)]
    public async Task<Wrapper> GetMyOrders([FromQuery] DataQueryRequest query) =>
        await orderService.GetOrders(query, this.UserId);
    
    [HttpGet("orders/subscription/plans/{plan}")]
    [RoleAuthorize(EnumRole.User)]
    [ProducesResponseType<WrapperGeneric<GetPlanExtras>>(200)]
    public async Task<Wrapper> GetPlanExtras(EnumSPlans plan, [FromQuery] DataQueryRequest query) =>
        await orderService.GetPlanExtras(plan, query);

    [HttpPost("orders/subscription")]
    [RoleAuthorize(EnumRole.User)]
    public async Task<Wrapper> CreateSubscriptionOrder([FromBody] CreateSubscriptionOrderDto dto) =>
        (await orderService.CreateSubscriptionOrder(this.UserId, dto), 200);

    [HttpDelete("orders/{orderId:long:min(1)}")]
    [RoleAuthorize(EnumRole.User)]
    public async Task<Wrapper> RemoveOrder(long orderId)
    {
        await orderService.Remove(this.UserId, orderId);
        return 200;
    }

    [HttpGet("orders/{orderId:long:min(1)}/payment-link")]
    [RoleAuthorize(EnumRole.User)]
    public async Task<Wrapper> MakePaymentLink(long orderId) =>
        (await orderService.MakePaymentLink(this.UserId, orderId), 200);

    #endregion

    #region Click

    [HttpPost("click")]
    [AllowAnonymous]
    public async Task<IActionResult> HandleClickRequest(
        [FromForm(Name = "click_trans_id")] long? clickTransId,
        [FromForm(Name = "service_id")] int? serviceId,
        [FromForm(Name = "click_paydoc_id")] long? clickPayDocId,
        [FromForm(Name = "merchant_trans_id")] string? merchantTransId,
        [FromForm(Name = "merchant_prepare_id")]
        uint? merchantPrepareId,
        [FromForm(Name = "amount")] decimal? amount,
        [FromForm(Name = "action")] int? action,
        [FromForm(Name = "error")] int? error,
        [FromForm(Name = "error_note")] string? errorNote,
        [FromForm(Name = "sign_time")] string? signTime,
        [FromForm(Name = "sign_string")] string? signString
    )
    {
        var request = new ClickRequest()
        {
            ClickTransId = clickTransId,
            ServiceId = serviceId,
            ClickPayDocId = clickPayDocId,
            OrderId = merchantTransId,
            MerchantPrepareId = merchantPrepareId,
            Amount = amount,
            Action = action,
            Error = error,
            ErrorNote = errorNote,
            SignTime = signTime,
            SignString = signString,
        };

        var response = await clickService.HandleAsync(request);

        return Ok(response);
    }

    #endregion

    #region Payme

    [HttpPost("payme")]
    [AllowAnonymous]
    public async Task<IActionResult> HandleClickRequest([FromBody] BaseRequest request)
    {
        var authHeaderRaw = this.Request.Headers.Authorization.ToString();
        var basicAuthToken = authHeaderRaw.Replace("Basic ", "");

        var response = await paymeService.HandleAsync(request, basicAuthToken);

        return Ok(response);
    }

    #endregion
}