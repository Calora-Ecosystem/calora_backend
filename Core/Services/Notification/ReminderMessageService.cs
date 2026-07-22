using BRB.Core.Common.Models;
using BRB.Core.EF.Attributes;
using BRB.Core.EF.Extensions;
using Core.Brokers.DbContext;
using Core.Entities.Notification;
using Core.Services.Notification.Contracts;
using Core.Services.Notification.Exceptions;
using Microsoft.EntityFrameworkCore;
using ResultWrapper.Library;

namespace Core.Services.Notification;

[Injectable]
public class ReminderMessageService(AppDbContext dbContext)
{
    public async Task<Wrapper> GetAll(DataQueryRequest q)
    {
        return await dbContext.ReminderMessages
            .Select(x => new GetReminderMessageShortDto(x.Id, x.Type, x.Menu, x.Title,
                x.Time == null ? (TimeOnly?)null : TimeOnly.FromTimeSpan(x.Time.Value), x.IsActive))
            .GetByDataQueryAsync(q);
    }

    public async Task<GetReminderMessageDto> GetById(long id)
    {
        return await dbContext.ReminderMessages
            .Where(x => x.Id == id)
            .Select(x => new GetReminderMessageDto(x.Id, x.Type, x.Menu, x.Title, x.Description,
                x.Time == null ? (TimeOnly?)null : TimeOnly.FromTimeSpan(x.Time.Value), x.IsActive))
            .FirstOrDefaultAsync() ?? throw new ReminderMessageNotFoundException();
    }

    public async Task<ReminderMessage> CreateOrUpdate(CreateOrUpdateReminderMessageDto dto)
    {
        var entity = dto.Id.HasValue
            ? await dbContext.ReminderMessages.GetByIdOrThrowsNotFoundException(dto.Id.Value)
            : await dbContext.ReminderMessages
                  .FirstOrDefaultAsync(x => x.Type == dto.Type && x.Menu == dto.Menu)
              ?? new ReminderMessage();

        entity.Type = dto.Type;
        entity.Menu = dto.Menu;
        entity.Title = dto.Title;
        entity.Description = dto.Description;
        entity.Time = dto.Time?.ToTimeSpan();
        entity.IsActive = dto.IsActive;

        entity = dbContext.Update(entity).Entity;
        await dbContext.SaveChangesAsync();

        return entity;
    }

    public async Task<int> Remove(long id)
    {
        return await dbContext.ReminderMessages
            .Where(x => x.Id == id)
            .ExecuteDeleteAsync();
    }
}
