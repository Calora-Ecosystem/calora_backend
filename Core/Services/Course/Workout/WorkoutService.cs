using BRB.Core.Common.Models;
using BRB.Core.EF.Attributes;
using BRB.Core.EF.Extensions;
using Core.Brokers.DbContext;
using Core.Entities.Course.Enum;
using Core.Services.Course.Workout.Contracts;
using ResultWrapper.Library;

namespace Core.Services.Course.Workout;

[Injectable]
public class WorkoutService(AppDbContext dbContext)
{
    public async Task<Wrapper> GetAll(long userId, DataQueryRequest query)
    {
        return await dbContext
            .Workouts
            .Select(x => new
            {
                x.Id,
                x.CourseId,
                x.Title,
                x.HasRest,
                TotalItems = x.Exercises.Count(),
                DoneItems = dbContext.Exercises
                    .Join(dbContext.CourseHistories,
                        e => e.Id,
                        h => h.EntityId,
                        (e, h) => new { e, h })
                    .Any(joined =>
                        joined.e.WorkoutId == x.Id &&
                        joined.h.UserId == userId &&
                        joined.h.Type == EnumHistoryEntityType.Workout),
                TotalDurationInMin = x.Exercises.Sum(exercise => exercise.Duration.TotalMinutes),
                TotalMetrics = x.Exercises.SelectMany(exercise => exercise.Metrics)
                    .GroupBy(metric => metric.Metric)
                    .Select(metrics => new
                    {
                        Metric = metrics.Key,
                        Sum = metrics.Sum(metric => metric.Value)
                    }),
            })
            .GetByDataQueryAsync(query);
    }

    public async Task CrateOrUpdate(CreateOrUpdateWorkoutDto dto)
    {
        var workout = dto.Id.HasValue
            ? await dbContext.Workouts.GetByIdOrThrowsNotFoundException(dto.Id.Value)
            : new Entities.Course.Workout();

        workout.CourseId = dto.CourseId;
        workout.Title = dto.Title;
        workout.HasRest = dto.HasRest;

        dbContext.Update(workout);
        await dbContext.SaveChangesAsync();
    }

    public async Task Remove(long id)
    {
        var workout = await dbContext.Workouts.GetByIdOrThrowsNotFoundException(id);

        dbContext.Remove(workout);
        await dbContext.SaveChangesAsync();
    }
}