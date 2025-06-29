using Core.Services.Auth;
using Core.Services.Auth.Contracts;
using Microsoft.AspNetCore.Mvc;
using ResultWrapper.Library;

namespace WebApi.Controllers;

[ApiController]
[Route("auth")]
public class AuthController(AuthService authService) : ControllerBase
{
    [HttpPost("registration")]
    public async Task<Wrapper> Register([FromBody] RegisterDto dto) =>
        (await authService.RegisterAsync(dto), 200);

    [HttpPost("sign-in")]
    public async Task<Wrapper> SignIn([FromBody] SignInDto dto) =>
        (await authService.SignInAsync(dto), 200);

    [HttpPost("send-otp/{email}")]
    public async Task<Wrapper> SendOtp(string email) =>
        (await authService.SendVerificationCode(email), 200);

    [HttpGet("roles")]
    public Wrapper Roles() =>
        (authService.GetAllRoles(), 200);
}