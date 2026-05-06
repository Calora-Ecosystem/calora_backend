using BRB.Core.Common.Models;
using Core.Attributes;
using Core.Enums;
using Core.Services.Notification;
using Core.Services.Notification.Contracts;
using Microsoft.AspNetCore.Mvc;
using ResultWrapper.Library;
using WebCore.Controller;

namespace WebApi.Controllers;

[Route(("reminder"))]
[ApiController]
[RoleAuthorize(EnumRole.User)]
public class ReminderController(ReminderService service, ReminderMessageService messageService) : AuthorizedController
{
    #region Reminder

    [HttpGet]
    [ProducesResponseType(typeof(WrapperGeneric<GetReminderDto>), 200)]
    public async Task<Wrapper> GetAll([FromQuery] DataQueryRequest q) =>
        await service.GetAllByUserId(this.UserId, q);

    [HttpPost]
    public async Task<Wrapper> AddReminder(AddRemindDto dto) => (await service.AddReminder(this.UserId, dto), 200);

    /// <summary>
    /// Remove a reminder
    /// </summary>
    /// <param name="id">Reminder ID</param>
    /// <returns></returns>
    [HttpDelete("{id:long:min(1)}")]
    public async Task<Wrapper> Remove([FromRoute] long id) =>
        (await service.Remove(this.UserId, id), 200);

    // [HttpGet("moments")]
    // public async Workout<Wrapper> GetAllMoments([FromQuery] DataQueryRequest q) =>
    //     await service.GetAllMoments(q);
    //
    // [HttpPost("moments")]
    // public async Workout<Wrapper> CreateMoment(CreateMomentDto dto) => (await service.CreateMoment(dto), 200);
    //
    // [HttpDelete("moments/{id:long:min(1)}")]
    // public async Workout<Wrapper> RemoveMoment([FromRoute] long id) =>
    //     (await service.RemoveMoment(id), 200);

    #endregion

    #region ReminderMessage

    [HttpGet("messages")]
    [RoleAuthorize(EnumRole.SuperAdmin)]
    [ProducesResponseType(typeof(WrapperGeneric<GetReminderMessageDto>), 200)]
    public async Task<Wrapper> GetAllMessages([FromQuery] DataQueryRequest q) =>
        await messageService.GetAll(q);

    [HttpGet("messages/{id:long:min(1)}")]
    [RoleAuthorize(EnumRole.SuperAdmin)]
    [ProducesResponseType(typeof(WrapperGeneric<GetReminderMessageDto>), 200)]
    public async Task<Wrapper> GetMessageById([FromRoute] long id) =>
        (await messageService.GetById(id), 200);

    [HttpPost("messages")]
    [RoleAuthorize(EnumRole.SuperAdmin)]
    public async Task<Wrapper> CreateOrUpdateMessage(CreateOrUpdateReminderMessageDto dto) =>
        (await messageService.CreateOrUpdate(dto), 200);

    [HttpDelete("messages/{id:long:min(1)}")]
    [RoleAuthorize(EnumRole.SuperAdmin)]
    public async Task<Wrapper> RemoveMessage([FromRoute] long id) =>
        (await messageService.Remove(id), 200);

    #endregion
}