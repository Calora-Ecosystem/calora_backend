using BRB.Core.Common.Models;
using Core.Attributes;
using Core.Enums;
using Core.Services.Coins;
using Core.Services.Coins.Contracts;
using Microsoft.AspNetCore.Mvc;
using ResultWrapper.Library;
using WebCore.Controller;

namespace WebApi.Controllers;

/// <summary>
/// Profil → "Do'stni taklif qilish".
/// </summary>
[ApiController]
[Route("referrals")]
[RoleAuthorize(EnumRole.User)]
public class ReferralController(ReferralService referralService) : AuthorizedController
{
    /// <summary>Taklif kodi, taklif qilinganlar, keyingi premiumgacha progress (5 do'st = 1 oy) va chegirma.</summary>
    [HttpGet("me")]
    [ProducesResponseType<WrapperGeneric<ReferralInfoDto>>(200)]
    public async Task<Wrapper> GetMy() => (await referralService.GetMy(this.UserId), 200);

    /// <summary>
    /// Ulashish uchun yangi taklif kodi (har ulashishda boshqa kod). Oldingi kodlar ham amal qiladi.
    /// </summary>
    [HttpPost("code")]
    [ProducesResponseType<WrapperGeneric<string>>(200)]
    public async Task<Wrapper> CreateCode() => (await referralService.CreateNewCode(this.UserId), 200);

    /// <summary>Taklif qilingan do'stlar va holati: <c>Joined</c> — ro'yxatdan o'tdi, <c>Active</c> — ilovaga kirdi.</summary>
    [HttpGet("invited")]
    [ProducesResponseType<WrapperGeneric<IEnumerable<ReferredFriendDto>>>(200)]
    public async Task<Wrapper> GetInvited([FromQuery] DataQueryRequest q) =>
        await referralService.GetInvited(this.UserId, q);

    /// <summary>
    /// Yangi user ro'yxatdan o'tgach do'stining kodini tasdiqlaydi ("men shu userdan kirdim").
    /// Taklif qilingan user birinchi premium xaridida chegirma oladi.
    /// </summary>
    [HttpPost("apply")]
    [ProducesResponseType<WrapperGeneric<ApplyReferralResultDto>>(200)]
    public async Task<Wrapper> Apply([FromBody] ApplyReferralCodeDto dto) =>
        (await referralService.Apply(this.UserId, dto), 200);
}
