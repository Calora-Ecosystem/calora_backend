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

    [AutomaticRetry(Attempts = 2)]
    public async Task EnqueueNotifications()
    {
        (await dbContext.PushNotifications
                .Where(x => !x.EnqueuedAt.HasValue)
                .OrderBy(x => x.CreatedAt)
                .ToListAsync())
            .ForEach(n =>
            {
                n.EnqueuedAt = DateTime.Now;
                if (n.Scheduled.HasValue)
                    BackgroundJob.Schedule<NotificationService>(x => x.SendPush(n.Id), n.Scheduled.Value);
                else
                    BackgroundJob.Enqueue<NotificationService>(x => x.SendPush(n.Id));
            });

        await dbContext.SaveChangesAsync();
    }
}