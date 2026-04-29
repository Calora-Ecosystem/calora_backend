using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using BRB.Core.Common.Attributes;
using Core.Attributes;
using WebApi.Exceptions;
using Core.Constants;
using Core.Enums;
using Core.Services.Auth;
using Core.Services.Auth.Contracts;
using Core.Services.Auth.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ResultWrapper.Library;
using WebCore.Controller;

namespace WebApi.Controllers;

[ApiController]
[Route("auth")]
[AllowAnonymous]
public class AuthController(AuthService authService) : AuthorizedController
{
    [HttpPost("registration/email")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public async Task<Wrapper> Register([FromBody] RegisterViaEmailDto dto) =>
        (await authService.RegisterViaEmailAsync(dto), 200);

    [HttpPost("registration/phone")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public async Task<Wrapper> Register([FromBody] RegisterViaPhoneDto dto) =>
        (await authService.RegisterViaPhoneAsync(dto), 200);

    /// <summary>
    /// Also create new user with verified email
    /// </summary>
    /// <param name="viaEmailDto"></param>
    /// <returns></returns>
    [HttpPost("sign-in/email")]
    public async Task<Wrapper> SignIn([FromBody] SignInViaEmailDto viaEmailDto) =>
        (await authService.SignInViaEmailAsync(viaEmailDto), 200);

    [HttpPost("sign-in/phone")]
    public async Task<Wrapper> SignIn([FromBody] SignInViaPhoneDto dto) =>
        (await authService.SignInViaPhoneAsync(dto), 200);

    [HttpPost("sign-in/google")]
    public async Task<Wrapper> SignInViaGoogle([FromBody] SsoSignInDto dto) =>
        (await authService.SignInWithGoogle(dto), 200);

    [HttpPost("sign-in/apple")]
    public async Task<Wrapper> SignInViaApple([FromBody] SsoSignInDto dto) =>
        (await authService.SignInWithAppleToken(dto), 200);

    [HttpGet("refresh-token")]
    public async Task<Wrapper>
        RefreshToken([FromQuery, Required] string rToken)
    {
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(this.Request.Headers.Authorization.ToString().Replace("Bearer ", ""));

        return (await authService.RefreshToken(
                !long.TryParse(jwt.Claims.FirstOrDefault(x => x.Type == CustomClaims.UserId)?.Value, out var userId)
                    ? throw new AccessTokenInvalidException()
                    : userId,
                rToken,
                !long.TryParse(jwt.Claims.FirstOrDefault(x => x.Type == CustomClaims.DeviceId)?.Value, out var deviceId)
                    ? throw new AccessTokenInvalidException()
                    : deviceId),
            200);
    }

    [HttpPost("send-otp/email/{email}")]
#if !DEBUG
    [EnableRateLimiting("otp_limit")]
#endif
    public async Task<Wrapper> SendOtp([EmailAddress] string email) =>
        (await authService.SendVerificationCode(EnumChannel.Email, email), 200);

    [HttpPost("send-otp/phone/{phone:required}")]
#if !DEBUG
    [EnableRateLimiting("otp_limit")]
#endif
    public async Task<Wrapper> SendOtpViaPhone(
        [FromRoute, LocalPhone, DefaultValue("+998998887766"), Required] string phone) =>
        (await authService.SendVerificationCode(EnumChannel.Phone, phone), 200);

    [HttpGet("roles")]
    [RoleAuthorize(EnumRole.SuperAdmin)]
    public Wrapper Roles() =>
        (authService.GetAllRoles(), 200);

    [AllowAnonymous]
    [HttpGet]
    public Wrapper Test()
    {
        return (new { Message = "Test" }, 200);
    }

    [HttpGet("logout")]
    public async Task<Wrapper> Logout()
    {
        await authService.Logout(this.User.Claims.ToArray());
        return 200;
    }
}