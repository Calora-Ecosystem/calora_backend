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

[Route(("notifications"))]
[ApiController]
[RoleAuthorize(EnumRole.User)]
public class NotificationController(NotificationService notificationService) : AuthorizedController
{
    [HttpGet]
    [ProducesResponseType(typeof(WrapperGeneric<IEnumerable<GetNotificationDto>>), 200)]
    public async Task<Wrapper> GetAll([FromQuery] DataQueryRequest q) =>
        await notificationService.GetAllNotifications(this.UserId, q);

    [HttpGet("unread")]
    [ProducesResponseType(typeof(WrapperGeneric<int>), 200)]
    public async Task<Wrapper> GetUnreadNotificationsCount() =>
        (await notificationService.GetUnreadNotificationsCount(this.UserId), 200);

    [HttpPut("mark-as-read/{notificationId:long:min(1)}")]
    public async Task<Wrapper> MarkRead(long notificationId)
    {
        await notificationService.MarkAsRead(this.UserId, notificationId);
        return 200;
    }

    [HttpPost]
    [ProducesResponseType(typeof(WrapperGeneric<int>), 200)]
    [RoleAuthorize(EnumRole.SuperAdmin)]
    public async Task<Wrapper> CreateOrUpdateNotification(PushNotificationDto dto)
    {
        await notificationService.CreateOrUpdatePushNotification(dto);
        return 200;
    }
}