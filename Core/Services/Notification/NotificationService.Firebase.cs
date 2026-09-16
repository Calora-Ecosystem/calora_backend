using System.Collections.ObjectModel;
using BRB.Core.Common.Exceptions;
using BRB.Core.Common.Extensions;
using BRB.Core.EF.Extensions;
using Core.Services.Notification.Contracts;
using FirebaseAdmin.Messaging;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace Core.Services.Notification;

public partial class NotificationService
{
    public async Task<SendPushResultDto> SendPush(List<string> tokens,
        FirebaseAdmin.Messaging.Notification notification, Dictionary<string, string>? meta)
    {
        var response = await FirebaseMessaging.DefaultInstance
            .SendEachForMulticastAsync(new MulticastMessage()
            {
                Notification = notification,
                Tokens = tokens,
                Data = meta,
                Android = new AndroidConfig()
                {
                    Priority = Priority.High,
                    Notification = new AndroidNotification() { Priority = NotificationPriority.HIGH }
                },
                Apns = new ApnsConfig()
                {
                    Headers = new Dictionary<string, string>()
                    {
                        { "apns-priority", "10" }
                    }
                }
            });

        return new SendPushResultDto
        {
            Total = tokens.Count,
            FailureCount = response.FailureCount,
            SuccessCount = response.SuccessCount
        };
    }

    [AutomaticRetry(Attempts = 3)]
    public async Task SendPush(long notificationId)
    {
        var notification = await dbContext.PushNotifications.GetByIdOrThrowsNotFoundException(notificationId);

        // Send-time gate: skip meal reminders if the user has already logged that menu today.
        if (notification.MealGateMenu.HasValue)
        {
            var today = DateTime.Now.Date;
            var alreadyLogged = await dbContext.DailyMenus
                .AnyAsync(m => m.UserId == notification.UserId
                               && m.Menu == notification.MealGateMenu.Value
                               && m.Date == today);

            if (alreadyLogged)
            {
                notification.SentAt = DateTime.Now;
                notification.SuccessCount = 0;
                notification.FailureCount = 0;
                await dbContext.SaveChangesAsync();
                return;
            }
        }

        var fcmTokens = await dbContext.Devices
            .Where(x => x.UserId == notification.UserId && x.IsActive && x.FcmToken != null)
            .Select(x => x.FcmToken!)
            .ToListAsync();

        if (fcmTokens.Count == 0)
        {
            notification.SentAt = DateTime.Now;
            notification.SuccessCount = 0;
            notification.FailureCount = 0;
        }
        else
        {
            try
            {
                var response = await SendPush(fcmTokens, new FirebaseAdmin.Messaging.Notification()
                {
                    Title = notification.Title,
                    Body = notification.Description,
                    ImageUrl = notification.Image
                }, notification.Meta);

                notification.SentAt = DateTime.Now;
                notification.SuccessCount = response.SuccessCount;
                notification.FailureCount = response.FailureCount;
            }
            catch (Exception e)
            {
                Log.Error(e, "Error while sending push notification");
                notification.SuccessCount = -1;
                notification.FailureCount = -1;
            }
        }

        await dbContext.SaveChangesAsync();
    }

    [AutomaticRetry(Attempts = 3)]
    public async Task SendBatchPush(List<long> notificationIds)
    {
        if (notificationIds == null || notificationIds.Count == 0)
            return;

        var notifications = await dbContext.PushNotifications
            .Where(x => notificationIds.Contains(x.Id) && !x.SentAt.HasValue)
            .ToListAsync();

        if (notifications.Count == 0)
            return;

        var now = DateTime.Now;
        var today = now.Date;

        // 1. Send-time gate for meal reminders: skip if user has already logged this menu today
        var gatedNotifications = notifications.Where(n => n.MealGateMenu.HasValue).ToList();
        if (gatedNotifications.Count > 0)
        {
            var gatedUserIds = gatedNotifications.Select(n => n.UserId).Distinct().ToList();
            var loggedMenus = await dbContext.DailyMenus
                .Where(m => gatedUserIds.Contains(m.UserId) && m.Date == today)
                .Select(m => new { m.UserId, m.Menu })
                .ToListAsync();

            var loggedSet = loggedMenus.Select(m => (m.UserId, m.Menu)).ToHashSet();
            var skippedIds = gatedNotifications
                .Where(n => loggedSet.Contains((n.UserId, n.MealGateMenu!.Value)))
                .Select(n => n.Id)
                .ToList();

            if (skippedIds.Count > 0)
            {
                await dbContext.PushNotifications
                    .Where(n => skippedIds.Contains(n.Id))
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(x => x.SentAt, now)
                        .SetProperty(x => x.SuccessCount, 0)
                        .SetProperty(x => x.FailureCount, 0));

                notifications.RemoveAll(n => skippedIds.Contains(n.Id));
            }
        }

        if (notifications.Count == 0)
            return;

        // 2. Fetch active FCM tokens (each user has at most 1 active token)
        var userIds = notifications.Select(n => n.UserId).Distinct().ToList();
        var activeDevices = await dbContext.Devices
            .Where(d => userIds.Contains(d.UserId) && d.IsActive && d.FcmToken != null)
            .Select(d => new { d.UserId, d.FcmToken })
            .ToListAsync();

        var tokenByUserId = activeDevices.ToDictionary(d => d.UserId, d => d.FcmToken!);

        // 3. Mark notifications for users without active tokens as sent immediately
        var noTokenNotificationIds = notifications
            .Where(n => !tokenByUserId.ContainsKey(n.UserId))
            .Select(n => n.Id)
            .ToList();

        if (noTokenNotificationIds.Count > 0)
        {
            await dbContext.PushNotifications
                .Where(n => noTokenNotificationIds.Contains(n.Id))
                .ExecuteUpdateAsync(s => s
                    .SetProperty(x => x.SentAt, now)
                    .SetProperty(x => x.SuccessCount, 0)
                    .SetProperty(x => x.FailureCount, 0));

            notifications.RemoveAll(n => noTokenNotificationIds.Contains(n.Id));
        }

        if (notifications.Count == 0)
            return;

        // 4. Group remaining notifications by identical payload (Title, Description, Image)
        var groups = notifications.GroupBy(n => new
        {
            n.Title,
            n.Description,
            n.Image
        });

        foreach (var group in groups)
        {
            var groupNotifications = group.ToList();
            var meta = groupNotifications.FirstOrDefault(n => n.Meta != null)?.Meta;

            var tokenNotificationPairs = groupNotifications
                .Where(n => tokenByUserId.ContainsKey(n.UserId))
                .Select(n => new { Token = tokenByUserId[n.UserId], NotificationId = n.Id })
                .ToList();

            const int firebaseChunkSize = 500;
            foreach (var chunk in tokenNotificationPairs.Chunk(firebaseChunkSize))
            {
                var chunkTokens = chunk.Select(x => x.Token).ToList();
                var chunkIds = chunk.Select(x => x.NotificationId).ToList();

                try
                {
                    var response = await SendPush(chunkTokens, new FirebaseAdmin.Messaging.Notification
                    {
                        Title = group.Key.Title,
                        Body = group.Key.Description,
                        ImageUrl = group.Key.Image
                    }, meta);

                    await dbContext.PushNotifications
                        .Where(n => chunkIds.Contains(n.Id))
                        .ExecuteUpdateAsync(s => s
                            .SetProperty(x => x.SentAt, now)
                            .SetProperty(x => x.SuccessCount, response.SuccessCount)
                            .SetProperty(x => x.FailureCount, response.FailureCount));
                }
                catch (Exception e)
                {
                    Log.Error(e, "Error while sending batch push notification for {Count} tokens", chunkTokens.Count);

                    await dbContext.PushNotifications
                        .Where(n => chunkIds.Contains(n.Id))
                        .ExecuteUpdateAsync(s => s
                            .SetProperty(x => x.SentAt, now)
                            .SetProperty(x => x.SuccessCount, -1)
                            .SetProperty(x => x.FailureCount, -1));
                }
            }
        }
    }

    [AutomaticRetry(Attempts = 2)]
    public async Task EnqueueNotifications()
    {
        var now = DateTime.Now;

        // 1. Process future-scheduled notifications that have not been enqueued yet
        var scheduledNotifications = await dbContext.PushNotifications
            .Where(x => !x.EnqueuedAt.HasValue && x.Scheduled.HasValue && x.Scheduled > now)
            .OrderBy(x => x.CreatedAt)
            .Take(1000)
            .ToListAsync();

        if (scheduledNotifications.Count > 0)
        {
            var scheduledIds = scheduledNotifications.Select(x => x.Id).ToList();
            await dbContext.PushNotifications
                .Where(x => scheduledIds.Contains(x.Id))
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.EnqueuedAt, now));

            var scheduledGroups = scheduledNotifications.GroupBy(x => x.Scheduled!.Value);
            foreach (var group in scheduledGroups)
            {
                foreach (var chunk in group.Select(x => x.Id).Chunk(500))
                {
                    BackgroundJob.Schedule<NotificationService>(x => x.SendBatchPush(chunk.ToList()), group.Key);
                }
            }
        }

        // 2. Process notifications ready to be sent now (not enqueued, or scheduled in the past)
        var pendingIds = await dbContext.PushNotifications
            .Where(x => !x.EnqueuedAt.HasValue && (!x.Scheduled.HasValue || x.Scheduled <= now))
            .OrderBy(x => x.CreatedAt)
            .Select(x => x.Id)
            .Take(1000)
            .ToListAsync();

        if (pendingIds.Count > 0)
        {
            await dbContext.PushNotifications
                .Where(x => pendingIds.Contains(x.Id))
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.EnqueuedAt, now));

            foreach (var chunk in pendingIds.Chunk(500))
            {
                BackgroundJob.Enqueue<NotificationService>(x => x.SendBatchPush(chunk.ToList()));
            }
        }
    }
}