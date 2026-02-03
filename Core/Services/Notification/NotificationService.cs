using System.Linq.Expressions;
using BRB.Core.Common.Exceptions;
using BRB.Core.Common.Models;
using BRB.Core.EF.Attributes;
using BRB.Core.EF.Extensions;
using Core.Brokers.DbContext;
using Core.Brokers.EmailBroker;
using Core.Brokers.EskizBroker;
using Core.Entities.Notification;
using Core.Enums;
using Core.Services.Notification.Contracts;
using FirebaseAdmin.Messaging;
using Microsoft.EntityFrameworkCore;
using ResultWrapper.Library;

namespace Core.Services.Notification;

[Injectable]
public partial class NotificationService(
    EmailClient emailClient,
    FirebaseMessaging firebase,
    AppDbContext dbContext,
    EskizClient eskizClient)
{
    public async Task<int> GetUnreadNotificationsCount(long userId)
    {
        return await dbContext.Notifications.CountAsync(x => x.UserId == userId && !x.HasRead);
    }

    public async Task<Wrapper> GetAllNotifications(long userId, DataQueryRequest q)
    {
        var query = dbContext.PushNotifications
            .Where(x => x.UserId == userId);


        return (await query
            .OrderByDescending(x => x.SentAt)
            .Page(q)
            .Select(x => new GetNotificationDto
            {
                Id = x.Id, UserId = x.UserId, Title = x.Title,
                Description = x.Description,
                Image = x.Image,
                HasRead = x.HasRead,
                SentAt = x.CreatedAt
            })
            .ToListAsync(), await query.CountAsync());
    }

    public async Task MarkAsRead(long userId, long id)
    {
        await dbContext
            .Notifications
            .Where(x => x.Id == id && x.UserId == userId)
            .ExecuteUpdateAsync(x =>
                x.SetProperty(notification => notification.HasRead, notification => true));
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

    public async Task FireForReminderEvent(Reminder reminder, DateTime? scheduled = null)
    {
        var message =
            await dbContext.ReminderMessages
                .FirstOrDefaultAsync(x => x.Type == reminder.Type && x.Menu == reminder.Menu) ?? new ReminderMessage()
            {
                Title = $"Reminding: {reminder.Type}{(reminder.Menu.HasValue ? $"-{reminder.Menu}" : "")}"
            };

        var notification = new PushNotificationDto()
        {
            UserId = reminder.UserId,
            Description = message.Description,
            Title = message.Title,
            Scheduled = scheduled
        };

        await this.CreateOrUpdatePushNotification(notification);
    }
}