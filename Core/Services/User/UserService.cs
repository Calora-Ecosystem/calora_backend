using BRB.Core.Common.Exceptions;
using BRB.Core.EF.Attributes;
using BRB.Core.EF.Extensions;
using Core.Brokers.DbContext;
using Core.Entities.Auth;
using Core.Enums;
using Core.Services.User.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Core.Services.User;

[Injectable]
public class UserService(AppDbContext context)
{
    public async Task<object> GetUserAsync(long userId)
    {
        var user = await context.Users
                       .Select(x => new
                       {
                           x.Id,
                           x.Email,
                           x.Roles
                       })
                       .FirstOrDefaultAsync(x => x.Id == userId)
                   ?? throw new NotFoundException("User not found.");

        return user;
    }

    #region UserExtras
    public async Task<object> GetExtra(long userId)
    {
        var extra = await context.UserExtras
            .Select(x => new
            {
                x.UserId,
                x.Weight,
                x.Height,
                x.Bmi,
                x.Gender,
                x.BirthDate,
                x.Photo,
                x.Name
            })
            .FirstOrDefaultAsync(x => x.UserId == userId)
                ?? throw new NotFoundException("User not found.");

        return extra;
    }

    public async Task CreateExtra(long userId, CreateUserExtraDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var purposes = await context.Purposes
            .Where(p => dto.PurposeIds.Contains(p.Id))
            .ToListAsync();

        var userExtra = new UserExtra
        {
            UserId = userId,
            Weight = dto.Weight,
            Height = dto.Height,
            Bmi = dto.Bmi,
            Gender = dto.Gender,
            BirthDate = dto.BirthDate,
            Photo = dto.Photo,
            Name = dto.Name,
            Language = dto.Language,
            Purposes = purposes
        };

        await context.UserExtras.AddAsync(userExtra);
        await context.SaveChangesAsync();
    }

    public async Task UpdateExtra(long userId, UpdateUserExtraDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var purposes = await context.Purposes
            .Where(p => dto.PurposeIds.Contains(p.Id))
            .ToListAsync();

        var userExtra = new UserExtra
        {
            Id = dto.Id,
            UserId = userId,
            Weight = dto.Weight,
            Height = dto.Height,
            Bmi = dto.Bmi,
            Gender = dto.Gender,
            BirthDate = dto.BirthDate,
            Photo = dto.Photo,
            Name = dto.Name,
            Language = dto.Language,
            Purposes = purposes
        };

        context.UserExtras.Update(userExtra);
        await context.SaveChangesAsync();
    }

    public async Task DeleteExtra(long userId)
    {
        var existing = await context.UserExtras
            .FirstOrDefaultAsync(x => x.UserId == userId)
            ?? throw new NotFoundException("User not found.");

        context.UserExtras.Remove(existing);
        await context.SaveChangesAsync();
    }
    #endregion

    #region UserNormsGeneral
    public async Task<object> GetNorm(long userId)
    {
        var norms = await context.UserNormsGeneral
            .Where(x => x.UserId == userId)
            .Select(x => new
            {
                x.Metric,
                x.Value
            })
            .ToListAsync();

        return norms;
    }

    public async Task CreateNorm(long userId, CreateUserNormDto dto)
    {
        var existing = await context.UserNormsGeneral
            .FirstOrDefaultAsync(x => x.UserId == userId && x.Metric == dto.Metric);

        if (existing is not null)
            throw new InvalidOperationException("This metric is available for this user");

        var userNorm = new UserNormGeneral
        {
            UserId = userId,
            Metric = dto.Metric,
            Value = dto.Value
        };

        await context.UserNormsGeneral.AddAsync(userNorm);
        await context.SaveChangesAsync();
    }

    public async Task UpdateNorm(long userId, EnumMetrics metric, UpdateUserNormDto dto)
    {
        var existing = await context.UserNormsGeneral
            .FirstOrDefaultAsync(x => x.UserId == userId && x.Metric == metric);

        if (existing is null)
            throw new KeyNotFoundException("UserNorm not found.");

        existing.Value = dto.Value;

        await context.SaveChangesAsync();
    }

    public async Task DeleteNorm(long userId, EnumMetrics metric)
    {
        var existing = await context.UserNormsGeneral
            .FirstOrDefaultAsync(x => x.UserId == userId && x.Metric == metric)
            ?? throw new NotFoundException("User or metric not found.");

        context.UserNormsGeneral.Remove(existing);
        await context.SaveChangesAsync();
    }
    #endregion

    #region UserDailies
    public async Task<object> GetDaily(long userId)
    {
        var dailies = await context.UserDailies
            .Where(x => x.UserId == userId)
            .Select(x => new
            {
                x.Date,
                x.Metric,
                x.Value
            })
            .ToListAsync();

        return dailies;
    }

    public async Task CreateDaily(long userId, CreateUserDailyDto dto)
    {
        var existing = await context.UserDailies
            .FirstOrDefaultAsync(x =>
                x.UserId == userId &&
                x.Metric == dto.Metric &&
                x.Date.Date == dto.Date.Date);

        if (existing is not null)
            throw new InvalidOperationException("Daily record already exists for this user and metric on the specified date.");

        var userDaily = new UserDaily
        {
            UserId = userId,
            Metric = dto.Metric,
            Date = dto.Date,
            Value = dto.Value
        };

        await context.UserDailies.AddAsync(userDaily);
        await context.SaveChangesAsync();
    }

    public async Task UpdateDaily(long userId, EnumMetrics metric, DateTime date, UpdateUserDailyDto dto)
    {
        var existing = await context.UserDailies
            .FirstOrDefaultAsync(x =>
                x.UserId == userId &&
                x.Metric == metric &&
                x.Date.Date == date.Date) ?? throw new NotFoundException("User daily record not found.");

        existing.Value = dto.Value;
        await context.SaveChangesAsync();
    }

    public async Task DeleteDaily(long userId, EnumMetrics metric, DateTime date)
    {
        var existing = await context.UserDailies
            .FirstOrDefaultAsync(x =>
                x.UserId == userId &&
                x.Metric == metric &&
                x.Date.Date == date)
            ?? throw new NotFoundException("User or daily record not found.");

        context.UserDailies.Remove(existing);
        await context.SaveChangesAsync();
    }
    #endregion

    public async Task AssignUserToRole(long userId, EnumRole role)
    {
        var user = await context.Users.GetByIdOrThrowsNotFoundException(userId);
        if (!user.Roles.Contains(role.ToString()))
            user.Roles.Add(role.ToString());

        context.Update(user);

        await context.SaveChangesAsync();
    }
}