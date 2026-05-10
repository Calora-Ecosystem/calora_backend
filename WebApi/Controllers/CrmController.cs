using Core.Attributes;
using Core.Enums;
using Core.Services.Crm;
using Core.Services.Crm.Contracts;
using Microsoft.AspNetCore.Mvc;
using ResultWrapper.Library;
using WebCore.Controller;

namespace WebApi.Controllers;

[ApiController]
[Route("crm")]
[RoleAuthorize(EnumRole.Operator)]
public class CrmController(LeadService leadService) : AuthorizedController
{
    #region Leads

    [HttpGet("leads")]
    [ProducesResponseType<WrapperGeneric<IEnumerable<GetLeadDto>>>(200)]
    public async Task<Wrapper> GetAllLeads([FromQuery] GetLeadsQuery q) =>
        await leadService.GetAllAsync(q);

    [HttpGet("leads/{id:long:min(1)}")]
    [ProducesResponseType<WrapperGeneric<GetLeadDetailDto>>(200)]
    public async Task<Wrapper> GetLeadById(long id) =>
        (await leadService.GetByIdAsync(id), 200);

    #endregion
    
    #region Notes

    [HttpGet("leads/{leadId:long:min(1)}/notes")]
    [ProducesResponseType<WrapperGeneric<IEnumerable<GetNoteDto>>>(200)]
    public async Task<Wrapper> GetNotesByLeadId(long leadId) =>
        (await leadService.GetNotesByLeadIdAsync(leadId), 200);

    [HttpPost("leads/{leadId:long:min(1)}/notes")]
    [ProducesResponseType<WrapperGeneric<GetNoteDto>>(200)]
    public async Task<Wrapper> UpsertNote(long leadId, [FromBody] UpsertNoteDto dto) =>
        (await leadService.CreateOrUpdateNote(leadId, this.UserId, dto), 200);

    [HttpDelete("notes/{noteId:long:min(1)}")]
    public async Task<Wrapper> DeleteNote(long noteId)
    {
        await leadService.DeleteNoteAsync(noteId, this.UserId);
        return 200;
    }

    #endregion
}