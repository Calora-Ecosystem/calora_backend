using BRB.Core.Common.Extensions;
using BRB.Core.Common.Models;
using BRB.Core.EF.Attributes;
using BRB.Core.EF.Extensions;
using Core.Brokers.DbContext;
using Core.Entities.Notification;
using Core.Enums;
using Core.Services.Notification.Contracts;
using Core.Services.Notification.Exceptions;
using Microsoft.EntityFrameworkCore;
using ResultWrapper.Library;

namespace Core.Services.Notification;

[Injectable]
public class ReminderService(AppDbContext dbContext, NotificationService notificationService)
{
    //>1 and <= 60
    public const int CheckReminderWindowInMin = 30;

    public async Task<Wrapper> GetAllByUserId(long userId, DataQueryRequest q)
    {
        return await dbContext.Reminders
            .Where(x => x.UserId == userId)
            .Select(x => new GetReminderDto(x.Id, x.Type, x.Menu, x.Time))
            .GetByDataQueryAsync(q);
    }

    public async Task<Reminder> AddReminder(long userId, AddRemindDto dto)
    {
        if (dto.Menu.HasValue && dto.Type != EnumMomentType.Food)
            throw new MenuOnlyForFoodException();
        
        var reminder =
            await dbContext.Reminders.FirstOrDefaultAsync(x => x.UserId == userId && x.Type == dto.Type &&
                                                               (dto.Type != EnumMomentType.Food ||
                                                                x.Menu == dto.Menu)) ??
            new Reminder()
            {
                UserId = userId,
                Type = dto.Type,
                Menu = dto.Menu
            };

        reminder.Time = dto.Time.ToTimeSpan();

        reminder = dbContext.Update(reminder).Entity;

        await dbContext.SaveChangesAsync();

        return reminder;
    }

    public async Task<int> Remove(long userId, long reminderId)
    {
        return await dbContext
            .Reminders
            .Where(x => x.Id == reminderId && x.UserId == userId)
            .ExecuteDeleteAsync();
    }

    /// <summary>
    /// Dispatches global, dashboard-scheduled meal reminders. For every active Food
    /// <see cref="ReminderMessage"/> whose time falls in the current window, creates a scheduled
    /// push for each user (with an active FCM device) who has not yet logged that menu today.
    /// A second send-time gate in <see cref="NotificationService.SendPush(long)"/> catches users
    /// who log the meal during the window.
    /// </summary>
    public async Task CheckMealReminders()
    {
        var now = DateTimeOffset.Now;
        var nowSpan = now.TimeOfDay;
        var windowEndSpan = now.AddMinutes(CheckReminderWindowInMin).TimeOfDay;
        var today = now.Date;

        var messages = await dbContext.ReminderMessages
            .Where(x => x.IsActive && x.Time != null && x.Type == EnumMomentType.Food && x.Menu != null
                        && x.Time > nowSpan && x.Time <= windowEndSpan)
            .ToListAsync();

        foreach (var msg in messages)
        {
            var menu = msg.Menu!.Value;
            var scheduled = now.Add(msg.Time!.Value - nowSpan).DateTime;

            var loggedUserIds = dbContext.DailyMenus
                .Where(m => m.Menu == menu && m.Date == today)
                .Select(m => m.UserId);

            var userIds = await dbContext.Devices
                .Where(d => d.IsActive && d.FcmToken != null && !loggedUserIds.Contains(d.UserId))
                .Select(d => d.UserId)
                .Distinct()
                .ToListAsync();

            if (userIds.Count == 0)
                continue;

            var pushes = userIds.Select(uid => new PushNotification
            {
                UserId = uid,
                Title = msg.Title,
                Description = msg.Description,
                Scheduled = scheduled,
                MealGateMenu = menu
            });

            await dbContext.PushNotifications.AddRangeAsync(pushes);
            await dbContext.SaveChangesAsync();
        }
    }

    public async Task CheckReminders()
    {
        var now = DateTimeOffset.Now;
        var nowSpan = now.TimeOfDay;
        var windowEnd = now.AddMinutes(CheckReminderWindowInMin);
        var windowEndSpan = windowEnd.TimeOfDay;

        await (await dbContext.Reminders
                .Where(x =>
                    x.Type == EnumMomentType.Water
                        ? x.Time.Hours != 0 && nowSpan.Hours % x.Time.Hours == 0
                        : x.Time > nowSpan && x.Time <= windowEndSpan
                )
                .ToListAsync())
            .ForEachAsync(x => notificationService.FireForReminderEvent(x,
                x.Type == EnumMomentType.Water ? null : now.Add(x.Time - nowSpan).DateTime));
    }

    // public async Workout<Wrapper> GetAllMoments(DataQueryRequest q)
    // {
    //     return await dbContext.Moments.Select(x => new { x.Id, x.Name }).GetByDataQueryAsync(q);
    // }
    //
    // public async Workout<int> RemoveMoment(long momentId)
    // {
    //     return await dbContext.Moments.Where(x => x.Id == momentId).ExecuteDeleteAsync();
    // }
    //
    // public async Workout<Moment> CreateMoment(CreateMomentDto dto)
    // {
    //     // var exists =
    //     //     await dbContext.Moments.AnyAsync(x => x.Time.Hour == dto.Time.Hour && x.Time.Minute == dto.Time.Minute);
    //     //
    //     // if (exists)
    //     //     throw new AlreadyExistsException("Moment already exists");
    //
    //     var moment = dbContext.Moments.Add(new Moment()
    //     {
    //         Name = dto.Name,
    //         Time = dto.Time,
    //     }).Entity;
    //
    //     await dbContext.SaveChangesAsync();
    //
    //     return moment;
    // }
}