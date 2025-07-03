using Core.Enums;
using Core.Services.User;
using Core.Services.User.Contracts;
using Microsoft.AspNetCore.Mvc;
using ResultWrapper.Library;
using WebCore.Controller;

namespace WebApi.Controllers;

[ApiController]
[Route("user")]
public class UserController(UserService userService) : AuthorizedController
{
    [HttpGet("{userId:long:min(1)}")]
    public async Task<Wrapper> GetById(long userId) =>
        (await userService.GetUserAsync(userId), 200);

    [HttpGet("me")]
    public async Task<Wrapper> GetMe() =>
        (await userService.GetUserAsync(this.UserId), 200);

    #region Extras
    [HttpGet("extras")]
    public async Task<Wrapper> GetExtras() =>
        (await userService.GetExtra(this.UserId), 200);

    [HttpPost("extras")]
    public async Task<Wrapper> AddExtra([FromBody] CreateUserExtraDto userExtra)
    {
        await userService.CreateExtra(this.UserId, userExtra);
        return (new { Message = "User extra added successfully." }, 201);
    }

    [HttpPut("extras")]
    public async Task<Wrapper> UpdateExtra([FromBody] UpdateUserExtraDto userExtra)
    {
        await userService.UpdateExtra(this.UserId, userExtra);
        return (new { Message = "User extra updated successfully." }, 200);
    }

    [HttpDelete("extras")]
    public async Task<Wrapper> DeleteExtra()
    {
        await userService.DeleteExtra(this.UserId);
        return (new { Message = "User extra deleted successfully." }, 200);
    }
    #endregion

    #region Norms
    [HttpGet("norms")]
    public async Task<Wrapper> GetNorms() =>
    (await userService.GetNorm(this.UserId), 200);

    [HttpPost("norms")]
    public async Task<Wrapper> AddNorm([FromBody] CreateUserNormDto userNorm)
    {
        await userService.CreateNorm(this.UserId, userNorm);
        return (new { Message = "User norm added successfully." }, 201);
    }

    [HttpPut("norms/{metric}")]
    public async Task<Wrapper> UpdateNorm([FromRoute] EnumMetrics metric, [FromBody] UpdateUserNormDto userNorm)
    {
        await userService.UpdateNorm(this.UserId, metric, userNorm);
        return (new { Message = "User norm updated successfully." }, 200);
    }

    [HttpDelete("norms/{metric}")]
    public async Task<Wrapper> DeleteNorm([FromRoute] EnumMetrics metric)
    {
        await userService.DeleteNorm(this.UserId, metric);
        return (new { Message = "User norm deleted successfully." }, 200);
    } 
    #endregion

    [HttpGet("dailies")]
    public async Task<Wrapper> GetDailies() =>
        (await userService.GetDaily(this.UserId), 200);

    [HttpPost("dailies")]
    public async Task<Wrapper> AddDaily([FromBody] CreateUserDailyDto userDaily)
    {
        await userService.CreateDaily(this.UserId, userDaily);
        return (new { Message = "User daily record added successfully." }, 201);
    }

    [HttpPut("dailies/{date}")]
    public async Task<Wrapper> UpdateDaily([FromRoute] EnumMetrics metric, [FromRoute] DateTime date, [FromBody] UpdateUserDailyDto userDaily)
    {
        await userService.UpdateDaily(this.UserId, metric, date, userDaily);
        return (new { Message = "User daily record updated successfully." }, 200);
    }

    [HttpDelete("dailies/{date}")]
    public async Task<Wrapper> DeleteDaily([FromRoute] EnumMetrics metric, [FromRoute] DateTime date)
    {
        await userService.DeleteDaily(this.UserId, metric, date);
        return (new { Message = "User daily record deleted successfully." }, 200);
    }

    [HttpGet("{userId:long}/assign-role")]
    public Wrapper AssignRole(long userId, EnumRole role) =>
        (userService.AssignUserToRole(userId, role), 200);
}