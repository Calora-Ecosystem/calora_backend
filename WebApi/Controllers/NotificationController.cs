using Core;
using Core.Enums;
using Core.Services.Notification;
using Microsoft.AspNetCore.Mvc;
using ResultWrapper.Library;
using WebCore.Controller;

namespace WebApi.Controllers;

[Route(("notifications"))]
[ApiController]
[RoleAuthorize(EnumRole.User)]
public class NotificationController(NotificationService notificationService) : AuthorizedController
{
    [HttpGet("unread")]
    [ProducesResponseType(typeof(WrapperGeneric<int>), 200)]
    public async Task<Wrapper> GetUnreadNotificationsCount() =>
        (await notificationService.GetUnreadNotificationsCount(this.UserId), 200);
}