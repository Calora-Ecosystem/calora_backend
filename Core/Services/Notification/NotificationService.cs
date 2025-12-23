using BRB.Core.Common.Exceptions;
using BRB.Core.EF.Attributes;
using BRB.Core.EF.Extensions;
using Core.Brokers.DbContext;
using Core.Brokers.EmailBroker;
using Core.Entities.Notification;
using Core.Services.Notification.Contracts;
using FirebaseAdmin.Messaging;
using Microsoft.EntityFrameworkCore;

namespace Core.Services.Notification;

[Injectable]
public partial class NotificationService(EmailClient emailClient, FirebaseMessaging firebase, AppDbContext dbContext)
{
    public async Task<int> GetUnreadNotificationsCount(long userId)
    {
        return await dbContext.Notifications.CountAsync(x => x.UserId == userId && x.HasRead);
    }

    public async Task CreateOrUpdatePushNotification(PushNotificationDto dto)
    {
        await dbContext.Users.ExistsOrThrowsNotFoundException(dto.UserId);

        var notification = dto.Id.HasValue
            ? await dbContext.PushNotifications
                  .FirstOrDefaultAsync(x => x.Id == dto.Id.Value && !x.SentAt.HasValue) ??
              throw new NotFoundException("Notification not found")
            : new PushNotification()
            {
                UserId = dto.UserId
            };

        notification.Title = dto.Title;
        notification.Image = dto.Image;
        notification.Description = dto.Description;
        notification.Scheduled = dto.Scheduled;
        notification.Meta = dto.Meta;

        dbContext.Update(notification);
        await dbContext.SaveChangesAsync();
    }
}