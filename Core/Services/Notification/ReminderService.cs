using BRB.Core.Common.Exceptions;
using BRB.Core.Common.Models;
using BRB.Core.EF.Attributes;
using BRB.Core.EF.Extensions;
using Core.Brokers.DbContext;
using Core.Entities.Notification;
using Core.Entities.Refs;
using Core.Services.Notification.Contracts;
using Microsoft.EntityFrameworkCore;
using ResultWrapper.Library;

namespace Core.Services.Notification;

[Injectable]
public class ReminderService(AppDbContext dbContext)
{
    public async Task<Wrapper> GetAllByUserId(long userId, DataQueryRequest q)
    {
        return await dbContext.Reminders
            .Where(x => x.UserId == userId)
            .Select(x => new
            {
                x.Id,
                x.MomentId,
                MomenName = x.Moment.Name,
                x.Before,
            })
            .GetByDataQueryAsync(q);
    }

    public async Task<Reminder> AddReminder(long userId, AddRemindDto dto)
    {
        await dbContext.Moments.ExistsOrThrowsNotFoundException(dto.MomentId);

        var reminder =
            await dbContext.Reminders.FirstOrDefaultAsync(x => x.UserId == userId && x.MomentId == dto.MomentId) ??
            new Reminder()
            {
                UserId = userId,
                MomentId = dto.MomentId
            };

        reminder.Before = TimeSpan.FromMinutes(dto.BeforeInMinutes);

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

    public async Task<Wrapper> GetAllMoments(DataQueryRequest q)
    {
        return await dbContext.Moments.Select(x => new { x.Id, x.Name }).GetByDataQueryAsync(q);
    }

    public async Task<int> RemoveMoment(long momentId)
    {
        return await dbContext.Moments.Where(x => x.Id == momentId).ExecuteDeleteAsync();
    }

    public async Task<Moment> CreateMoment(CreateMomentDto dto)
    {
        var exists =
            await dbContext.Moments.AnyAsync(x => x.Time.Hour == dto.Time.Hour && x.Time.Minute == dto.Time.Minute);

        if (exists)
            throw new AlreadyExistsException("Moment already exists");

        var moment = dbContext.Moments.Add(new Moment()
        {
            Name = dto.Name,
            Time = dto.Time,
        }).Entity;

        await dbContext.SaveChangesAsync();

        return moment;
    }
}