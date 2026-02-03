using BRB.Core.Common.Extensions;
using BRB.Core.Common.Models;
using BRB.Core.EF.Attributes;
using BRB.Core.EF.Extensions;
using Core.Brokers.DbContext;
using Core.Entities.Notification;
using Core.Enums;
using Core.Services.Notification.Contracts;
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

    public async Task CheckReminders()
    {
        var now = DateTime.Now;
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
            .ForEachAsync(x => notificationService.CreateOrUpdatePushNotification(new PushNotificationDto()
            {
                UserId = x.UserId,
                Description = "",
                Title = $"Reminding: {x.Type}{(x.Menu.HasValue ? $"-{x.Menu}" : "")}",
                Scheduled = x.Type == EnumMomentType.Water ? null : now.Add(x.Time - nowSpan)
            }));
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