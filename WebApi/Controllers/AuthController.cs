using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using BRB.Core.Common.Exceptions;
using Core;
using Core.Constants;
using Core.Enums;
using Core.Services.Auth;
using Core.Services.Auth.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ResultWrapper.Library;
using WebCore.Controller;

namespace WebApi.Controllers;

[ApiController]
[Route("auth")]
[AllowAnonymous]
public class AuthController(AuthService authService) : AuthorizedController
{
    [HttpPost("registration")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public async Task<Wrapper> Register([FromBody] RegisterDto dto) =>
        (await authService.RegisterAsync(dto), 200);

    /// <summary>
    /// Also create new user with verified email
    /// </summary>
    /// <param name="dto"></param>
    /// <returns></returns>
    [HttpPost("sign-in")]
    public async Task<Wrapper> SignIn([FromBody] SignInDto dto) =>
        (await authService.SignInAsync(dto), 200);

    [HttpGet("refresh-token")]
    public async Task<Wrapper>
        RefreshToken([FromQuery, Required] string rToken)
    {
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(this.Request.Headers.Authorization.ToString().Replace("Bearer ", ""));

        return (await authService.RefreshToken(
                !long.TryParse(jwt.Claims.FirstOrDefault(x => x.Type == CustomClaims.UserId)?.Value, out var userId)
                    ? throw new BadRequestException("Access token invalid")
                    : userId,
                rToken,
                !long.TryParse(jwt.Claims.FirstOrDefault(x => x.Type == CustomClaims.DeviceId)?.Value, out var deviceId)
                    ? throw new BadRequestException("Access token invalid")
                    : deviceId),
            200);
    }

    [HttpPost("send-otp/{email}")]
    public async Task<Wrapper> SendOtp([EmailAddress] string email) =>
        (await authService.SendVerificationCode(email), 200);

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