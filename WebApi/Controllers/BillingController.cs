using Core;
using Core.Attributes;
using Core.Enums;
using Core.Services.Billing;
using Core.Services.Billing.Click;
using Core.Services.Billing.Click.Contracts;
using Core.Services.Billing.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ResultWrapper.Library;
using WebCore.Controller;

namespace WebApi.Controllers;

[ApiController]
[Route("billing")]
public class BillingController(OrderService orderService, ClickService clickService) : AuthorizedController
{
    #region Orders

    [HttpPost("order/subscription")]
    [RoleAuthorize(EnumRole.User)]
    public async Task<Wrapper> CreateSubscriptionOrder([FromBody] CreateSubscriptionOrderDto dto) =>
        (await orderService.CreateSubscriptionOrder(this.UserId, dto), 200);

    [HttpDelete("order/{orderId:long:min(1)}")]
    [RoleAuthorize(EnumRole.User)]
    public async Task<Wrapper> RemoveOrder(long orderId)
    {
        await orderService.Remove(this.UserId, orderId);
        return 200;
    }

    [HttpGet("order/{orderId:long:min(1)}/payment-link")]
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
}