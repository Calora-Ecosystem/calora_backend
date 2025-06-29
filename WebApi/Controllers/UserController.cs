using Core.Services;
using Microsoft.AspNetCore.Mvc;
using ResultWrapper.Library;
using WebCore.Controller;

namespace WebApi.Controllers;

[ApiController]
[Route("user")]
public class UserController(UserService userService) : AuthorizedController
{
    [HttpGet("{userId:long}")]
    public async Task<Wrapper> GetMe(long userId) =>
        (await userService.GetMeAsync(userId), 200);
    
    [HttpGet("me")]
    public async Task<Wrapper> GetMe() =>
        (await userService.GetMeAsync(this.UserId), 200);
}