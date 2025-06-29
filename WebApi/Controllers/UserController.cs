using Core.Services;
using Microsoft.AspNetCore.Mvc;
using ResultWrapper.Library;
using WebCore.Controller;

namespace WebApi.Controllers;

[ApiController]
[Route("user")]
public class UserController(UserService userService) : AuthorizedController
{
    [HttpGet]
    public async Task<Wrapper> GetMe() =>
        (await userService.GetMeAsync(this.UserId), 200);
}