using BRB.Core.Common.Models;
using Core;
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
public class ReminderController(ReminderService service) : AuthorizedController
{
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
}