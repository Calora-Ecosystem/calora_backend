using System.Data;
using BRB.Core.Common.Exceptions;
using BRB.Core.Common.Models;
using BRB.Core.EF.Attributes;
using BRB.Core.EF.Extensions;
using Core.Brokers.DbContext;
using Core.Entities.Auth;
using Core.Enums;
using Core.Services.User.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ResultWrapper.Library;

namespace Core.Services.User;

[Injectable]
public class UserService(AppDbContext context)
{
    public async Task<object> GetUserAsync(long userId)
    {
        var user = await context.Users
                       .Where(x => x.Id == userId)
                       .Select(x => new GetUserDto(x.Id, x.Email ?? x.Phone, x.Roles))
                       .FirstOrDefaultAsync()
                   ?? throw new NotFoundException("User not found.");

        return user;
    }

    #region UserExtras

    public async Task<object> GetExtra(long userId)
    {
        var extra = await context.UserExtras
                        .Where(x => x.UserId == userId)
                        .Select(x => new GetUserExtraDto(x.UserId, x.Weight, x.EntryWeight, x.Height,
                            Math.Round(x.Bmi, 0), x.Gender,
                            x.BirthDate,
                            x.Photo, x.Name, x.ActivityLevel, x.Purpose, x.PhysicalActivity))
                        .FirstOrDefaultAsync()
                    ?? throw new NotFoundException("User not found.");

        extra.Progress = await UserProgressSummary(userId);

        return extra;
    }

    public async Task CreateOrUpdateExtra(long userId, CreateUserExtraDto dto)
    {
        var extra = await context.UserExtras
            .FirstOrDefaultAsync(x => x.UserId == userId) ?? new UserExtra()
        {
            UserId = userId,
            EntryWeight = dto.Weight
        };

        await context.Transactional(async () =>
        {
            extra.UserId = userId;
            extra.Height = dto.Height;
            extra.Bmi = dto.Weight / Math.Pow(dto.Height / 100, 2);
            extra.Gender = dto.Gender;
            extra.BirthDate = dto.BirthDate;
            extra.Photo = dto.Photo;
            extra.Name = dto.Name;
            extra.Language = dto.Language;
            extra.ActivityLevel = dto.ActivityLevel;
            extra.PhysicalActivity = dto.PhysicalActivity;

            if (extra.Id == 0 || dto.Purpose != extra.Purpose)
            {
                extra.EntryWeight = dto.Weight;
                extra.Purpose = dto.Purpose;
                await context.UserDailies
                    .Where(x => x.UserId == userId
                                && x.Metric == EnumMetrics.Weight)
                    .ExecuteDeleteAsync();
            }
            else
            {
                var progress = Math.Abs(dto.Weight - extra.Weight);
                await context.UserDailies.AddAsync(new UserDaily()
                {
                    Metric = EnumMetrics.Weight,
                    Date = DateTime.Now.Date,
                    Value = progress,
                    UserId = userId
                });
            }

            extra.Weight = dto.Weight;

            //Calculate user norms
            //Weight
            await CreateOrUpdateNorm(userId, new CreateUserNormDto()
            {
                Metric = EnumMetrics.Weight,
                Value = dto.TargetWeight
            });

            //Step
            await CreateOrUpdateNorm(userId, new CreateUserNormDto()
            {
                Metric = EnumMetrics.Step,
                Value = dto.Purpose switch
                {
                    EnumPurpose.WeightLoss => 8_000,
                    _ => 6_000
                }
            });

            //Water
            await CreateOrUpdateNorm(userId, new CreateUserNormDto()
            {
                Metric = EnumMetrics.Water,
                Value = dto.Purpose switch
                {
                    EnumPurpose.WeightLoss => extra.Weight * 30,
                    EnumPurpose.SaveCurrent => extra.Weight * 30,
                    _ => extra.Weight * 35
                }
            });

            var tdee = CalculateTdee(extra);

            //kcal
            await CreateOrUpdateNorm(userId, new CreateUserNormDto()
            {
                Metric = EnumMetrics.Kcal,
                Value = dto.Purpose switch
                {
                    EnumPurpose.WeightLoss => tdee - 500,
                    EnumPurpose.SaveCurrent => tdee,
                    _ => tdee + 300
                }
            });

            var heightInM = extra.Height / 100;

            //protein
            var protein = extra.Gender switch
            {
                EnumGender.Male => extra.Purpose switch
                {
                    EnumPurpose.WeightLoss => 1.6 * 24.9 * heightInM,
                    EnumPurpose.SaveCurrent => 1.8 * 24.9 * heightInM,
                    EnumPurpose.MuscleDevelopment => 2 * 24.9 * heightInM,
                    _ => 0
                },
                EnumGender.Female => extra.Purpose switch
                {
                    EnumPurpose.WeightLoss => 1.4 * 24.9 * heightInM,
                    EnumPurpose.SaveCurrent => 1.6 * 24.9 * heightInM,
                    EnumPurpose.MuscleDevelopment => 1.8 * 24.9 * heightInM,
                    _ => 0
                },
                _ => 0
            };

            await CreateOrUpdateNorm(userId, new CreateUserNormDto()
            {
                Metric = EnumMetrics.Protein,
                Value = protein
            });

            //fat
            var fat = extra.Gender switch
            {
                EnumGender.Male => extra.Purpose switch
                {
                    EnumPurpose.WeightLoss => 1 * 24.9 * heightInM,
                    EnumPurpose.SaveCurrent => 1.2 * 24.9 * heightInM,
                    EnumPurpose.MuscleDevelopment => 1.5 * 24.9 * heightInM,
                    _ => 0
                },
                EnumGender.Female => extra.Purpose switch
                {
                    EnumPurpose.WeightLoss => 0.8 * 24.9 * heightInM,
                    EnumPurpose.SaveCurrent => 1.0 * 24.9 * heightInM,
                    EnumPurpose.MuscleDevelopment => 1.2 * 24.9 * heightInM,
                    _ => 0
                },
                _ => 0
            };
            await CreateOrUpdateNorm(userId, new CreateUserNormDto()
            {
                Metric = EnumMetrics.Fat,
                Value = fat
            });

            //carb
            await CreateOrUpdateNorm(userId, new CreateUserNormDto()
            {
                Metric = EnumMetrics.Carb,
                Value = (2.5 + 0.5 * (extra.ActivityLevel - EnumActivityLevel.Minimal)) * extra.Weight
            });

            context.UserExtras.Update(extra);

            var user = await context.Users.GetByIdOrThrowsNotFoundException(userId);

            user.Name = extra.Name;
            context.Update(user);

            await context.SaveChangesAsync();
        });
    }

    // public async Task UpdateExtra(long userId, UpdateUserExtraDto dto)
    // {
    //     ArgumentNullException.ThrowIfNull(dto);
    //
    //     // var purposes = await context.Purposes
    //     //     .Where(p => dto.PurposeIds.Contains(p.Id))
    //     //     .ToListAsync();
    //     //
    //     // var userExtra = new UserExtra
    //     // {
    //     //     Id = dto.Id,
    //     //     UserId = userId,
    //     //     Weight = dto.Weight,
    //     //     Height = dto.Height,
    //     //     Bmi = dto.Bmi,
    //     //     Gender = dto.Gender,
    //     //     BirthDate = dto.BirthDate,
    //     //     Photo = dto.Photo,
    //     //     Name = dto.Name,
    //     //     Language = dto.Language,
    //     //     Purposes = purposes,
    //     //     ActivityLevel = dto.ActivityLevel
    //     // };
    //     //
    //     // context.UserExtras.Update(userExtra);
    //     // await context.SaveChangesAsync();
    // }

    public async Task DeleteExtra(long userId)
    {
        var existing = await context.UserExtras
                           .FirstOrDefaultAsync(x => x.UserId == userId)
                       ?? throw new NotFoundException("User not found.");

        context.UserExtras.Remove(existing);
        await context.SaveChangesAsync();
    }

    private async Task<List<UserProgressSummaryDto>> UserProgressSummary(long userId)
    {
        return await context.UserNorms
            .AsSplitQuery()
            .Where(x => x.UserId == userId)
            .GroupJoin(context.UserDailies
                    .Where(x => x.UserId == userId)
                    .GroupBy(x => x.Metric)
                    .Select(x => new
                    {
                        Metric = x.Key,
                        Sum = Math.Round(x.Sum(daily => daily.Value), 0)
                    }), general => general.Metric, arg => arg.Metric,
                (general, arg2) => new UserProgressSummaryDto
                {
                    Metric = general.Metric, Target = Math.Round(general.Value, 0),
                    Progress = !arg2.IsNullOrEmpty() ? arg2.First().Sum : 0
                })
            .ToListAsync();
    }

    #endregion

    #region UserNormsGeneral

    public async Task<Wrapper> GetNorm(long userId, DataQueryRequest q, EnumMetrics? metrics = null)
    {
        var norms = await context.UserNorms
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .Where(x => metrics == null || x.Metric == metrics)
            .Select(x => new GetNormDto(x.UserId, x.Metric, x.Value))
            .GetByDataQueryAsync(q);

        return norms;
    }

    public async Task CreateOrUpdateNorm(long userId, CreateUserNormDto dto)
    {
        var existing = await context.UserNorms
            .FirstOrDefaultAsync(x => x.UserId == userId && x.Metric == dto.Metric) ?? new UserNormGeneral()
        {
            UserId = userId,
            Metric = dto.Metric,
        };

        existing.Value = dto.Value;

        context.UserNorms.Update(existing);
        await context.SaveChangesAsync();
    }

    public async Task DeleteNorm(long userId, EnumMetrics metric)
    {
        var existing = await context.UserNorms
                           .FirstOrDefaultAsync(x => x.UserId == userId && x.Metric == metric)
                       ?? throw new NotFoundException("User or metric not found.");

        context.UserNorms.Remove(existing);
        await context.SaveChangesAsync();
    }

    #endregion

    #region UserDailies

    public async Task<Wrapper> GetDaily(long userId, DataQueryRequest q, EnumMetrics metrics,
        DateTime? from = null, DateTime? to = null)
    {
        from ??= DateTime.Now.Date;
        to ??= DateTime.Now.Date.AddDays(1);

        var query = context.UserDailies
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .Where(x => x.Metric == metrics && x.Date >= from && x.Date <= to);

        var byDate = await query
            .Select(x => new GetDailyDto
            {
                Date = x.Date, Metric = x.Metric, Value = x.Value
            })
            .FilterByExpressions(q.FilteringExpression)
            .Sort(q)
            .ToDictionaryAsync(x => x.Date, x => x);

        var result = Enumerable.Range(0, (to - from).Value.Days + 1).Select((_, i) =>
            byDate.TryGetValue(from.Value.AddDays(i), out var value)
                ? value
                : new GetDailyDto()
                {
                    Metric = metrics,
                    Value = 0,
                    Date = from.Value.AddDays(i)
                }).ToArray();

        return (result, result.Length);
    }

    public async Task CreateOrUpdateDaily(long userId, CreateUserDailyDto dto)
    {
        var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            var existing = await context.UserDailies
                .FirstOrDefaultAsync(x =>
                    x.UserId == userId &&
                    x.Metric == dto.Metric &&
                    x.Date.Date == dto.Date.Date) ?? context.Add(new UserDaily()
            {
                UserId = userId,
                Metric = dto.Metric,
                Date = dto.Date.Date,
            }).Entity;

            existing.Value = Math.Max(dto.Value, existing.Value); //daily qiymat faqat oshib borishi lozim.
            
            await context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task ResetDaily(long userId, DateTime date)
    {
        await context
            .UserDailies
            .Where(x => x.UserId == userId && x.Date.Date == date.Date)
            .ExecuteDeleteAsync();
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
select ung.user_id, sum(ung.value), count(ung.id) from user_dailies ung 
where ung.""date"" >= {from} and ung.""date"" <= {to} and metric = {EnumMetrics.Step}
group by ung.user_id
) sub 
")
            .LeftJoin2(context.UserExtras, stat => stat.UserId, extra => extra.UserId, (x, extra) =>
                new GetStepStatDto()
                {
                    User = new UserDto(
                        x.User.Id,
                        x.User.Name,
                        x.User.Email,
                        extra != null
                            ? new ExtraDto
                            (
                                extra.Photo,
                                extra.ActivityLevel
                            )
                            : null
                    ),
                    Sum = x.Sum, Count = x.Count, Index = x.Index
                }
            )
            .OrderBy(x => x.Index)
            .GetByDataQueryAsync(q);
    }

    public async Task<GetStepMetricsDto> CalculateStepMetrics(long userId, DateTime? from, DateTime? to)
    {
        from ??= DateTime.Now.Date;
        to ??= DateTime.Now.Date.AddDays(1);

        var extra = await context.UserExtras
            .FirstOrDefaultAsync(x => x.UserId == userId) ?? throw new NotFoundException("User extra not found");

        var totalFoots = Math.Round(await context.UserDailies
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .Where(x => x.Metric == EnumMetrics.Step && x.Date >= from && x.Date <= to)
            .SumAsync(x => x.Value), 1);

        const double mPerKm = 1000;

        var distance = Math.Round((extra.Gender == EnumGender.Male ? 0.8 : 0.7 /*m*/) * totalFoots, 1);
        var kcal = Math.Round(
            extra.Weight * (distance / mPerKm /* convert to km */) * (extra.Gender == EnumGender.Male ? 1.06 : 0.98),
            1);
        var duration = Math.Round((distance / mPerKm /* convert to km */) / 5.1, 1); // 5.1 km/hour; duration is hour

        return new GetStepMetricsDto(userId, totalFoots, distance / mPerKm /* convert to km */, kcal, duration);
    }

    #endregion

    #region Calculations

    public double CalculateTdee(UserExtra extra)
    {
        var bmr = extra.Gender switch
        {
            EnumGender.Male => 10 * extra.Weight + 6.25 * extra.Height - 5 * extra.Age + 5,
            _ => 10 * extra.Weight + 6.25 * extra.Height - 5 * extra.Age - 161
        };

        const double activityValueDistancePerLevel = 0.175;
        const double activityValueMin = 1.2;

        var activityValue = (extra.ActivityLevel - EnumActivityLevel.Minimal) * activityValueDistancePerLevel +
                            activityValueMin;

        var tdee = activityValue * bmr;

        return tdee;
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