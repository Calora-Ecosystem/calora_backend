using Core.Attributes;
using Core.Enums;
using Core.Services.StepGroups;
using Core.Services.StepGroups.Contracts;
using Microsoft.AspNetCore.Mvc;
using ResultWrapper.Library;
using WebCore.Controller;

namespace WebApi.Controllers;

/// <summary>
/// Qadam guruhlari: yaratish, kod orqali qo'shilish, guruh ichidagi qadam reytingi.
/// <c>from</c>/<c>to</c> berilmasa — bugun.
/// </summary>
[ApiController]
[Route("step-groups")]
[RoleAuthorize(EnumRole.User)]
public class StepGroupController(StepGroupService stepGroupService) : AuthorizedController
{
    [HttpGet]
    [ProducesResponseType<WrapperGeneric<IEnumerable<StepGroupDto>>>(200)]
    public async Task<Wrapper> GetMy([FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        var groups = await stepGroupService.GetMy(this.UserId, from, to);
        return (groups, groups.Count);
    }

    [HttpGet("{groupId:long:min(1)}")]
    [ProducesResponseType<WrapperGeneric<StepGroupDetailDto>>(200)]
    public async Task<Wrapper> GetById(long groupId, [FromQuery] DateTime? from, [FromQuery] DateTime? to) =>
        (await stepGroupService.GetById(this.UserId, groupId, from, to), 200);

    [HttpPost]
    [ProducesResponseType<WrapperGeneric<StepGroupDetailDto>>(200)]
    public async Task<Wrapper> Create([FromBody] CreateStepGroupDto dto) =>
        (await stepGroupService.Create(this.UserId, dto), 200);

    [HttpPost("join")]
    [ProducesResponseType<WrapperGeneric<StepGroupDetailDto>>(200)]
    public async Task<Wrapper> Join([FromBody] JoinStepGroupDto dto) =>
        (await stepGroupService.Join(this.UserId, dto), 200);

    /// <summary>Faqat admin — guruhni o'chirish.</summary>
    [HttpDelete("{groupId:long:min(1)}")]
    public async Task<Wrapper> Delete(long groupId)
    {
        await stepGroupService.Delete(this.UserId, groupId);
        return 200;
    }

    /// <summary>Guruhdan chiqish (admin chiqsa adminlik eng eski a'zoga o'tadi).</summary>
    [HttpPost("{groupId:long:min(1)}/leave")]
    public async Task<Wrapper> Leave(long groupId)
    {
        await stepGroupService.Leave(this.UserId, groupId);
        return 200;
    }

    /// <summary>Faqat admin — a'zoni chiqarish.</summary>
    [HttpDelete("{groupId:long:min(1)}/members/{memberUserId:long:min(1)}")]
    public async Task<Wrapper> RemoveMember(long groupId, long memberUserId)
    {
        await stepGroupService.RemoveMember(this.UserId, groupId, memberUserId);
        return 200;
    }
}
