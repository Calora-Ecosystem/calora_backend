using System.ComponentModel.DataAnnotations;
using Core;
using Core.Enums;
using Core.Services.Auth;
using Core.Services.Auth.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ResultWrapper.Library;
using WebCore.Enum;

namespace WebApi.Controllers;

[ApiController]
[Route("auth")]
[AllowAnonymous]
public class AuthController(AuthService authService) : ControllerBase
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
}