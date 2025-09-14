using BRB.Core.Common.Exceptions;
using BRB.Core.Common.Models;
using BRB.Core.EF.Attributes;
using BRB.Core.EF.Extensions;
using Core.Brokers.DbContext;
using Core.Entities.Auth;
using Core.Enums;
using Core.Services.User.Contracts;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Npgsql;
using ResultWrapper.Library;
using Index = System.Index;

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

    public async Task CreateOrUpdateNorm(long userId, CreateUserNormDto dto)
    {
        var existing = await context.UserNormsGeneral
            .FirstOrDefaultAsync(x => x.UserId == userId && x.Metric == dto.Metric) ?? new UserNormGeneral()
        {
            UserId = userId,
            Metric = dto.Metric,
        };

        existing.Value = dto.Value;

        context.UserNormsGeneral.Update(existing);
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

    public async Task<Wrapper> GetDaily(long userId, EnumMetrics? metrics, DataQueryRequest q)
    {
        var query = context.UserDailies
            .Where(x => x.UserId == userId);

        if (metrics is not null)
            query = query.Where(x => x.Metric == metrics);
            
        return await query
            .Select(x => new
            {
                x.Date,
                x.Metric,
                x.Value
            })
            .GetByDataQueryAsync(q);
    }

    public async Task CreateOrUpdateDaily(long userId, CreateUserDailyDto dto)
    {
        var existing = await context.UserDailies
            .FirstOrDefaultAsync(x =>
                x.UserId == userId &&
                x.Metric == dto.Metric &&
                x.Date.Date == dto.Date.Date) ?? new UserDaily()
        {
            UserId = userId,
            Metric = dto.Metric,
            Date = dto.Date.Date,
        };

        existing.Value = dto.Value;

        context.UserDailies.Update(existing);
        await context.SaveChangesAsync();
    }

    // public async Task UpdateDaily(long userId, EnumMetrics metric, DateTime date, UpdateUserDailyDto dto)
    // {
    //     var existing = await context.UserDailies
    //         .FirstOrDefaultAsync(x =>
    //             x.UserId == userId &&
    //             x.Metric == metric &&
    //             x.Date.Date == date.Date) ?? throw new NotFoundException("User daily record not found.");
    //
    //     existing.Value = dto.Value;
    //     await context.SaveChangesAsync();
    // }

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

    #region Step

    public async Task<Wrapper> StepStat(DateTime? from, DateTime? to, DataQueryRequest q)
    {
        from ??= DateTime.Now.Date;
        to ??= DateTime.Now.Date.AddDays(1);

        return await context.UserStepStats
            .FromSql(@$"
select sub.user_id , sub.sum, sub.count, ROW_NUMBER() OVER (ORDER BY sub.sum desc, sub.count desc) as index from (
select ung.user_id, sum(ung.value), count(ung.id) from user_norms_general ung 
where ung.""date"" >= {from} and ung.""date"" <= {to} and metric = {EnumMetrics.Step}
group by ung.user_id
) sub
")
            .LeftJoin2(context.UserExtras, stat => stat.UserId, extra => extra.UserId, (x, extra) =>
                new
                {
                    User = new
                    {
                        x.User.Id,
                        x.User.Name,
                        x.User.Email,
                        Extra = extra != null
                            ? new
                            {
                                extra.Photo,
                                extra.ActivityLevel
                            }
                            : null
                    },
                    x.Sum,
                    x.Count,
                    x.Index
                }
            )
            .GetByDataQueryAsync(q);
    }

    public async Task<Wrapper> CalculateStepMetrics(DateTime? from, DateTime? to, DataQueryRequest q)
    {
        from ??= DateTime.Now.Date;
        to ??= DateTime.Now.Date.AddDays(1);

        return await context
            .UserDailies
            .AsNoTracking()
            .Where(x => x.Metric == EnumMetrics.Step && x.Date >= from && x.Date <= to)
            .GroupBy(x => x.User, (user, dailies) => new
            {
                User = new
                {
                    user.Id,
                    user.Name,
                    user.Email,
                    Extra = user.Extra != null
                        ? new
                        {
                            user.Extra.Photo,
                        }
                        : null
                },
                Foots = dailies.Sum(x => x.Value),
                Distance =
                    (user.Extra != null ? user.Extra.Gender == EnumGender.Male ? 0.8 : 0.7 /*m*/ : 0.6 /*avarage m*/) *
                    dailies.Sum(x => x.Value),
                Kcal = user.Extra != null
                    ? user.Extra.Weight *
                      (user.Extra != null ? user.Extra.Gender == EnumGender.Male ? 0.8 : 0.7 /*m*/ : 0.6 /*avarage m*/
                      ) *
                      Math.Pow(dailies.Sum(x => x.Value), 2)
                    : 0
            })
            .AsSplitQuery()
            .GetByDataQueryAsync(q);
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