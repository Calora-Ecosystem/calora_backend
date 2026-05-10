using System.Text.Json;
using BRB.Core.Common.Models;
using WebApi.Exceptions;
using Core.Attributes;
using Core.Enums;
using Core.Services.Billing;
using Core.Services.Billing.Click;
using Core.Services.Billing.Click.Contracts;
using Core.Services.Billing.Contracts;
using Core.Services.Billing.Payme;
using Core.Services.Billing.Payme.Contracts;
using Core.Services.Billing.Rc;
using Core.Services.Billing.Rc.Contracts;
using Core.Services.Crm;
using Core.Services.Crm.Enum;
using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ResultWrapper.Library;
using WebCore.Controller;

namespace WebApi.Controllers;

[ApiController]
[Route("billing")]
public class BillingController(
    OrderService orderService,
    ClickService clickService,
    PaymeService paymeService,
    RcService rcService,
    CouponService couponService)
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
    public async Task<Wrapper> GetMyOrders([FromQuery] DataQueryRequest query){
        BackgroundJob.Enqueue<LeadService>(service => service.HandleEventAsync(new Core.Services.Crm.Contracts.HandleLeadEventDto(this.UserId, EnumLeadEvent.SubscriptionOpened)));
        return await orderService.GetOrders(query, this.UserId);
    }

    [HttpGet("orders/subscription/plans/{plan}")]
    [RoleAuthorize(EnumRole.User)]
    [ProducesResponseType<WrapperGeneric<IEnumerable<GetPlanExtras>>>(200)]
    public async Task<Wrapper> GetPlanExtras(EnumSPlans plan, [FromQuery] DataQueryRequest query) =>
        await orderService.GetPlanExtras(plan, query);

    [HttpPost("orders/subscription")]
    [RoleAuthorize(EnumRole.User)]
    [ProducesResponseType<WrapperGeneric<CreateSubscriptionOrderResponseDto>>(200)]
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

    #region Coupons

    [HttpGet("coupons")]
    [RoleAuthorize(EnumRole.SuperAdmin)]
    [ProducesResponseType<WrapperGeneric<IEnumerable<GetCouponsDto>>>(200)]
    public async Task<Wrapper> GetAllCoupons([FromQuery] DataQueryRequest query) =>
        await couponService.GetAll(query);

    [HttpGet("coupons/{couponId:long:min(1)}")]
    [RoleAuthorize(EnumRole.SuperAdmin)]
    [ProducesResponseType<WrapperGeneric<GetCouponByIdDto>>(200)]
    public async Task<Wrapper> GetCouponById(long couponId) =>
        (await couponService.GetById(couponId), 200);

    [HttpGet("coupons/{couponId:long:min(1)}/usages")]
    [RoleAuthorize(EnumRole.SuperAdmin)]
    [ProducesResponseType<WrapperGeneric<IEnumerable<GetCouponUsagesDto>>>(200)]
    public async Task<Wrapper> GetAllCouponUsages(long couponId, [FromQuery] DataQueryRequest query) =>
        await couponService.GetCouponUsages(couponId, query);

    [HttpGet("coupons/check")]
    [RoleAuthorize(EnumRole.User)]
    [ProducesResponseType<WrapperGeneric<CheckCouponDto>>(200)]
    public async Task<Wrapper> CheckCoupon([FromQuery] string code) =>
        (await couponService.CheckCoupon(this.UserId, code), 200);

    [HttpPost("coupons")]
    [RoleAuthorize(EnumRole.SuperAdmin)]
    public async Task<Wrapper> CreateOrUpdateCoupon([FromBody] CreateOrUpdateCouponDto dto)
    {
        await couponService.CreateOrUpdate(dto);
        return 200;
    }

    #endregion

    #region Rc

    [HttpPost("rc")]
    [AllowAnonymous]
    public async Task<Wrapper> HandleRcRequest()
    {
        rcService.ValidateAuthentication(this.Request.Headers.Authorization.ToString());

        using var reader = new StreamReader(Request.Body);
        var json = await reader.ReadToEndAsync();

        var request = JsonSerializer.Deserialize<RcRequest>(json, new JsonSerializerOptions()
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            PropertyNameCaseInsensitive = true
        });

        if (request is null)
            throw new InvalidRequestException();

        await rcService.HandleRequest(request);
        return 200;
    }

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
    public async Task<IActionResult> HandlePaymeRequest([FromBody] BaseRequest request,
        ILogger<BillingController> logger)
    {
        var authHeaderRaw = this.Request.Headers.Authorization.ToString();
        var basicAuthToken = authHeaderRaw.Replace("Basic ", "");

        var response = await paymeService.HandleAsync(request, basicAuthToken);

        logger.LogInformation("Payme response:\n{@Response}", response);

        return Ok(response);
    }

    #endregion
}