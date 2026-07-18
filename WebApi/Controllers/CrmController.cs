using Core.Attributes;
using Core.Entities.Crm.Enum;
using Core.Enums;
using Core.Services.Crm;
using Core.Services.Crm.Contracts;
using Microsoft.AspNetCore.Mvc;
using ResultWrapper.Library;
using WebCore.Controller;

namespace WebApi.Controllers;

[ApiController]
[Route("crm")]
[RoleAuthorize(EnumRole.Operator, EnumRole.HeadOfSales, EnumRole.SuperAdmin)]
public class CrmController(LeadService leadService, CrmStatsService statsService, CrmOperatorService operatorService) : AuthorizedController
{
    /// <summary>
    /// Lead scope for the caller. A multi-role account is scoped to the role it signed in as
    /// (X-Active-Role header): when acting as Operator it only sees/acts on its own leads, even
    /// if it also holds HeadOfSales/SuperAdmin. HeadOfSales/SuperAdmin otherwise see everything.
    /// </summary>
    private long? OperatorScope
    {
        get
        {
            var activeRole = Request.Headers["X-Active-Role"].FirstOrDefault();
            if (activeRole == nameof(EnumRole.Operator))
                return this.UserId;

            return User.IsInRole(nameof(EnumRole.HeadOfSales)) || User.IsInRole(nameof(EnumRole.SuperAdmin))
                ? null
                : this.UserId;
        }
    }

    /// <summary>Current user's name + roles. Available to every authenticated role.</summary>
    [HttpGet("me")]
    [RoleAuthorize(EnumRole.SuperAdmin, EnumRole.HeadOfSales, EnumRole.Operator, EnumRole.User)]
    [ProducesResponseType<WrapperGeneric<GetMeDto>>(200)]
    public async Task<Wrapper> GetMe() =>
        (await operatorService.GetMeAsync(this.UserId), 200);

    #region Leads

    [HttpGet("leads")]
    [ProducesResponseType<WrapperGeneric<IEnumerable<GetLeadDto>>>(200)]
    public async Task<Wrapper> GetAllLeads([FromQuery] GetLeadsQuery q) =>
        await leadService.GetAllAsync(q, OperatorScope);

    [HttpGet("leads/{id:long:min(1)}")]
    [ProducesResponseType<WrapperGeneric<GetLeadDetailDto>>(200)]
    public async Task<Wrapper> GetLeadById(long id) =>
        (await leadService.GetByIdAsync(id, OperatorScope), 200);

    [HttpGet("leads/{id:long:min(1)}/timeline")]
    [ProducesResponseType<WrapperGeneric<IEnumerable<GetLeadActivityDto>>>(200)]
    public async Task<Wrapper> GetLeadTimeline(long id) =>
        (await leadService.GetTimelineAsync(id), 200);

    [HttpPatch("leads/{id:long:min(1)}/status")]
    [RoleAuthorize(EnumRole.Operator)]
    public async Task<Wrapper> MoveStatus(long id, [FromBody] MoveStatusDto dto)
    {
        await leadService.MoveStatusAsync(id, this.UserId, dto.Status, dto.Reason);
        return 200;
    }

    [HttpPost("leads/{id:long:min(1)}/contact")]
    [RoleAuthorize(EnumRole.Operator)]
    public async Task<Wrapper> Contact(long id)
    {
        await leadService.ContactAsync(id, this.UserId);
        return 200;
    }

    /// <summary>Manually (re)assign a lead to an operator. HeadOfSales / SuperAdmin only.</summary>
    [HttpPatch("leads/{id:long:min(1)}/assign")]
    [RoleAuthorize(EnumRole.HeadOfSales, EnumRole.SuperAdmin)]
    public async Task<Wrapper> Assign(long id, [FromBody] AssignLeadDto dto)
    {
        await leadService.AssignToOperatorAsync(id, dto.OperatorId, this.UserId);
        return 200;
    }

    #endregion

    #region Notes

    [HttpGet("leads/{leadId:long:min(1)}/notes")]
    [ProducesResponseType<WrapperGeneric<IEnumerable<GetNoteDto>>>(200)]
    public async Task<Wrapper> GetNotesByLeadId(long leadId) =>
        (await leadService.GetNotesByLeadIdAsync(leadId), 200);

    [HttpPost("leads/{leadId:long:min(1)}/notes")]
    [RoleAuthorize(EnumRole.Operator)]
    [ProducesResponseType<WrapperGeneric<long>>(200)]
    public async Task<Wrapper> UpsertNote(long leadId, [FromBody] UpsertNoteDto dto) =>
        (await leadService.CreateOrUpdateNote(leadId, this.UserId, dto), 200);

    [HttpDelete("notes/{noteId:long:min(1)}")]
    [RoleAuthorize(EnumRole.Operator)]
    public async Task<Wrapper> DeleteNote(long noteId)
    {
        await leadService.DeleteNoteAsync(noteId, this.UserId);
        return 200;
    }

    #endregion

    #region Follow-ups

    [HttpPost("leads/{leadId:long:min(1)}/followups")]
    [RoleAuthorize(EnumRole.Operator)]
    [ProducesResponseType<WrapperGeneric<long>>(200)]
    public async Task<Wrapper> CreateFollowUp(long leadId, [FromBody] CreateFollowUpDto dto) =>
        (await leadService.CreateFollowUpAsync(leadId, this.UserId, dto), 200);

    [HttpPatch("followups/{followUpId:long:min(1)}/complete")]
    [RoleAuthorize(EnumRole.Operator)]
    public async Task<Wrapper> CompleteFollowUp(long followUpId)
    {
        await leadService.CompleteFollowUpAsync(followUpId, this.UserId);
        return 200;
    }

    [HttpGet("followups")]
    [RoleAuthorize(EnumRole.Operator)]
    [ProducesResponseType<WrapperGeneric<IEnumerable<GetFollowUpDto>>>(200)]
    public async Task<Wrapper> GetFollowUps([FromQuery] string? scope) =>
        (await leadService.GetFollowUpsAsync(this.UserId, scope), 200);

    #endregion

    #region Operator self stats

    [HttpGet("me/dashboard")]
    [RoleAuthorize(EnumRole.Operator)]
    [ProducesResponseType<WrapperGeneric<OperatorDashboardDto>>(200)]
    public async Task<Wrapper> GetMyDashboard() =>
        (await statsService.GetOperatorDashboardAsync(this.UserId), 200);

    [HttpGet("me/stats")]
    [RoleAuthorize(EnumRole.Operator)]
    [ProducesResponseType<WrapperGeneric<OperatorStatsDto>>(200)]
    public async Task<Wrapper> GetMyStats([FromQuery] EnumStatsPeriod period = EnumStatsPeriod.Month) =>
        (await statsService.GetOperatorStatsAsync(this.UserId, period), 200);

    /// <summary>What the current operator did on a given day (defaults to today).</summary>
    [HttpGet("me/day-log")]
    [RoleAuthorize(EnumRole.Operator)]
    [ProducesResponseType<WrapperGeneric<OperatorDayLogDto>>(200)]
    public async Task<Wrapper> GetMyDayLog([FromQuery] DateTime? date) =>
        (await statsService.GetDayLogAsync(this.UserId, date ?? DateTime.Now), 200);

    #endregion
}
