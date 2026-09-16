using System.Linq.Expressions;
using BRB.Core.Common.Models;
using Core.Exceptions;
using Core.Services.Notification.Exceptions;
using BRB.Core.EF.Attributes;
using BRB.Core.EF.Extensions;
using Core.Brokers.DbContext;
using Core.Brokers.EmailBroker;
using Core.Brokers.EskizBroker;
using Core.Entities.Notification;
using Core.Enums;
using Core.Services.Notification.Contracts;
using FirebaseAdmin.Messaging;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ResultWrapper.Library;

namespace Core.Services.Notification;

[Injectable]
public partial class NotificationService(
    EmailClient emailClient,
    FirebaseMessaging firebase,
    AppDbContext dbContext,
    EskizClient eskizClient,
    ILogger<NotificationService> logger)
{
    public async Task<int> GetUnreadNotificationsCount(long userId)
    {
        return await dbContext.Notifications.CountAsync(x => x.UserId == userId && !x.HasRead);
    }

    public async Task<Wrapper> GetAllNotifications(long userId, DataQueryRequest q)
    {
        var query = dbContext.PushNotifications
            .Where(x => x.UserId == userId && x.SentAt.HasValue);

        return (await query
            .OrderByDescending(x => x.SentAt)
            .Page(q)
            .Select(x => new GetNotificationDto
            {
                Id = x.Id, UserId = x.UserId, Title = x.Title,
                Description = x.Description,
                Image = x.Image,
                HasRead = x.HasRead,
                SentAt = x.SentAt!.Value
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
    
    public async Task MarkAsReadAll(long userId)
    {
        await dbContext
            .Notifications
            .Where(x => x.UserId == userId && !x.HasRead)
            .ExecuteUpdateAsync(x =>
                x.SetProperty(notification => notification.HasRead, notification => true));
    }

    public async Task CreateOrUpdatePushNotification(PushNotificationDto dto)
    {
        await dbContext.Users.ExistsOrThrowsNotFoundException(dto.UserId);

        var notification = dto.Id.HasValue
            ? await dbContext.PushNotifications
                  .FirstOrDefaultAsync(x => x.Id == dto.Id.Value && !x.SentAt.HasValue) ??
              throw new NotificationNotFoundException()
            : new PushNotification()
            {
                UserId = dto.UserId
            };

        notification.Title = dto.Title;
        notification.Image = dto.Image;
        notification.Description = dto.Description;
        notification.Scheduled = dto.Scheduled;
        notification.Meta = dto.Meta;
        notification.MealGateMenu = dto.MealGateMenu;

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
            Scheduled = scheduled,
            // Food reminders with a menu are gated: only delivered if the user hasn't logged that meal.
            MealGateMenu = reminder.Type == EnumMomentType.Food ? reminder.Menu : null
        };

        await this.CreateOrUpdatePushNotification(notification);
    }

    public async Task<int> SendBatchPushNotifications(BatchPushNotificationDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Title))
            throw new BadRequestException("title_required");

        List<long> targetUserIds;
        if (dto.AllUsers)
        {
            targetUserIds = await dbContext.Users
                .Select(x => x.Id)
                .ToListAsync();
        }
        else
        {
            if (dto.UserIds == null || dto.UserIds.Count == 0)
                throw new BadRequestException("user_ids_required");

            var distinctIds = dto.UserIds.Distinct().ToList();
            targetUserIds = await dbContext.Users
                .Where(x => distinctIds.Contains(x.Id))
                .Select(x => x.Id)
                .ToListAsync();
        }

        if (targetUserIds.Count == 0)
            return 0;

        var createdIds = new List<long>();
        const int batchSize = 1000;
        var now = DateTime.Now;

        foreach (var chunk in targetUserIds.Chunk(batchSize))
        {
            var notifications = chunk.Select(userId => new PushNotification
            {
                UserId = userId,
                Title = dto.Title,
                Description = dto.Description,
                Image = dto.Image,
                Meta = dto.Meta,
                Scheduled = dto.Scheduled,
                MealGateMenu = dto.MealGateMenu,
                HasRead = false,
                EnqueuedAt = now
            }).ToList();

            await dbContext.PushNotifications.AddRangeAsync(notifications);
            await dbContext.SaveChangesAsync();
            createdIds.AddRange(notifications.Select(x => x.Id));
            dbContext.ChangeTracker.Clear();
        }

        const int hangfireBatchSize = 500;
        foreach (var chunk in createdIds.Chunk(hangfireBatchSize))
        {
            var chunkList = chunk.ToList();
            if (dto.Scheduled.HasValue && dto.Scheduled > now)
            {
                BackgroundJob.Schedule<NotificationService>(x => x.SendBatchPush(chunkList), dto.Scheduled.Value);
            }
            else
            {
                BackgroundJob.Enqueue<NotificationService>(x => x.SendBatchPush(chunkList));
            }
        }

        return targetUserIds.Count;
    }
}