using BRB.Core.Common.Extensions;
using BRB.Core.Common.Models;
using Core;
using Core.Enums;
using Core.Services.User;
using Core.Services.User.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ResultWrapper.Library;
using WebCore.Controller;
using WebCore.Enum;

namespace WebApi.Controllers;

[ApiController]
[Route("users")]
[RoleAuthorize(EnumRole.User)]
public class UserController(UserService userService) : AuthorizedController
{
    [HttpGet("{userId:long:min(1)}")]
    [ProducesResponseType<WrapperGeneric<GetUserDto>>(200)]
    public async Task<Wrapper> GetById(long userId) =>
        (await userService.GetUserAsync(userId), 200);

    [HttpGet("me")]
    [ProducesResponseType<WrapperGeneric<GetUserDto>>(200)]
    public async Task<Wrapper> GetMe() =>
        (await userService.GetUserAsync(this.UserId), 200);

    #region Extras

    [HttpGet("extras")]
    [ProducesResponseType<WrapperGeneric<GetUserExtraDto>>(200)]
    public async Task<Wrapper> GetExtras() =>
        (await userService.GetExtra(this.UserId), 200);

    [HttpPost("extras")]
    public async Task<Wrapper> AddExtra([FromBody] CreateUserExtraDto userExtra)
    {
        await userService.CreateOrUpdateExtra(this.UserId, userExtra);
        return (new { Message = "User extra added successfully." }, 201);
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
    [ProducesResponseType<GetNormDto>(200)]
    public async Task<Wrapper> GetNorms([FromQuery] DataQueryRequest q, [FromQuery] EnumMetrics? metrics = null) =>
        await userService.GetNorm(this.UserId, q, metrics);

    [HttpPost("norms")]
    public async Task<Wrapper> AddNorm([FromBody] CreateUserNormDto userNorm)
    {
        await userService.CreateOrUpdateNorm(this.UserId, userNorm);
        return (new { Message = "User norm added successfully." }, 201);
    }

    [HttpDelete("norms/{metric}")]
    public async Task<Wrapper> DeleteNorm([FromRoute] EnumMetrics metric)
    {
        await userService.DeleteNorm(this.UserId, metric);
        return (new { Message = "User norm deleted successfully." }, 200);
    }

    #endregion

    #region Daily

    [HttpGet("dailies")]
    [ProducesResponseType<WrapperGeneric<GetDailyDto>>(200)]
    public async Task<Wrapper> GetDailies([FromQuery] DataQueryRequest q, [FromQuery] EnumMetrics metrics, [FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null) =>
        await userService.GetDaily(this.UserId, q, metrics, from, to);

    [HttpPost("dailies")]
    public async Task<Wrapper> AddDaily([FromBody] CreateUserDailyDto userDaily)
    {
        await userService.CreateOrUpdateDaily(this.UserId, userDaily);
        return (new { Message = "User daily record added successfully." }, 201);
    }
    
    [HttpPost("dailies/batch")]
    public async Task<Wrapper> AddDailyBatch([FromBody] List<CreateUserDailyDto> userDaily)
    {
        await userDaily.ForEachAsync(AddDaily);
        return (new { Message = "User daily record added successfully." }, 201);
    }

    // [HttpPut("dailies/{date}")]
    // public async Task<Wrapper> UpdateDaily([FromRoute] EnumMetrics metric, [FromRoute] DateTime date,
    //     [FromBody] UpdateUserDailyDto userDaily)
    // {
    //     await userService.UpdateDaily(this.UserId, metric, date, userDaily);
    //     return (new { Message = "User daily record updated successfully." }, 200);
    // }

    [HttpDelete("dailies/{date}")]
    public async Task<Wrapper> DeleteDaily([FromRoute] EnumMetrics metric, [FromRoute] DateTime date)
    {
        await userService.DeleteDaily(this.UserId, metric, date);
        return (new { Message = "User daily record deleted successfully." }, 200);
    }

    #endregion

    [HttpGet("steps/stat")]
    [ProducesResponseType<WrapperGeneric<GetStepStatDto>>(200)]
    public Task<Wrapper> GetStepStat([FromQuery] DateTime? from, [FromQuery] DateTime? to,
        [FromQuery] DataQueryRequest q) => userService.StepStat(from, to, q);

    [HttpGet("steps/metrics")]
    [ProducesResponseType<WrapperGeneric<GetStepMetricsDto>>(200)]
    public Task<Wrapper> CalculateStepMetrics([FromQuery] DateTime? from, [FromQuery] DateTime? to,
        [FromQuery] DataQueryRequest q, [FromQuery] long? userId = null) =>
        userService.CalculateStepMetrics(userId ?? this.UserId, from, to, q);

    [HttpGet("{userId:long:min(1)}/assign-role")]
    [Authorize(Policy = nameof(EnumAuthPolicies.SuperAdmin))]
    public Wrapper AssignRole(long userId, EnumRole role) =>
        (userService.AssignUserToRole(userId, role), 200);
}