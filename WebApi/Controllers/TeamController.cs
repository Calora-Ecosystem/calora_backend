using Core.Attributes;
using Core.Enums;
using Core.Services.User;
using Core.Services.User.Contracts;
using Microsoft.AspNetCore.Mvc;
using ResultWrapper.Library;
using WebCore.Controller;

namespace WebApi.Controllers;

/// <summary>Jamoa (xodimlar) boshqaruvi. Faqat SuperAdmin uchun.</summary>
[ApiController]
[Route("team")]
[RoleAuthorize(EnumRole.SuperAdmin)]
public class TeamController(TeamService teamService) : AuthorizedController
{
    [HttpGet]
    [ProducesResponseType<WrapperGeneric<IEnumerable<TeamMemberDto>>>(200)]
    public async Task<Wrapper> GetAll() => (await teamService.GetAllAsync(), 200);

    [HttpPost]
    [ProducesResponseType<WrapperGeneric<long>>(200)]
    public async Task<Wrapper> Create([FromBody] CreateTeamMemberDto dto) =>
        (await teamService.CreateAsync(dto), 200);

    [HttpPut("{memberId:long:min(1)}")]
    public async Task<Wrapper> Update(long memberId, [FromBody] UpdateTeamMemberDto dto)
    {
        await teamService.UpdateAsync(this.UserId, memberId, dto);
        return 200;
    }

    [HttpDelete("{memberId:long:min(1)}")]
    public async Task<Wrapper> Delete(long memberId)
    {
        await teamService.DeleteAsync(this.UserId, memberId);
        return 200;
    }
}
