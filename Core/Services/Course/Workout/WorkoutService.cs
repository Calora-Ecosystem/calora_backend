using BRB.Core.Common.Exceptions;
using BRB.Core.Common.Models;
using BRB.Core.EF.Attributes;
using BRB.Core.EF.Extensions;
using Core.Brokers.DbContext;
using Core.Entities.Course;
using Core.Entities.Course.Enum;
using Core.Enums;
using Core.Services.Course.Common;
using Core.Services.Course.Workout.Contracts;
using Microsoft.EntityFrameworkCore;
using ResultWrapper.Library;

namespace Core.Services.Course.Workout;

[Injectable]
public class WorkoutService(AppDbContext dbContext)
{
    public async Task<Wrapper> GetAll(long userId, DataQueryRequest query, long? courseId = null)
    {
        var q = dbContext.Workouts.AsQueryable();

        if (courseId is not null)
            q = q.Where(x => x.CourseId == courseId);

        return await q
            .Select(x => new GetWorkoutDto()
            {
                Id = x.Id,
                CourseId = x.CourseId,
                Title = x.Title, 
                HasRest = x.HasRest,
                TotalItems = x.Exercises.Count(),
                DoneItems = dbContext.Exercises
                    .Where(exercise => exercise.WorkoutId == exercise.Id)
                    .Join(dbContext.CourseItemStates,
                        e => e.Id,
                        h => h.EntityId,
                        (e, state) => new { e, state })
                    .Count(joined =>
                        joined.state.UserId == userId &&
                        joined.state.Type == EnumHistoryEntityType.Exercise),
                TotalDurationInMin = x.Exercises.Sum(exercise => exercise.Duration.TotalMinutes),
                TotalMetrics = x.Exercises
                    .Where(exercise => exercise.WorkoutId == x.Id)
                    .SelectMany(exercise => exercise.Metrics)
                    .GroupBy(metric => metric.Metric)
                    .Select(metrics => new GetWorkoutMetricDto
                    {
                        Metric = metrics.Key,
                        Sum = metrics.Sum(metric => metric.Value)
                    }),
                Order = x.Order
            })
            .OrderBy(x => x.Order)
            .GetByDataQueryAsync(query);
    }

    public async Task<long> CrateOrUpdate(CreateOrUpdateWorkoutDto dto)
    {
        if (!dbContext.Courses.Any(x => x.Id == dto.CourseId && x.Type == EnumCourseType.Workout))
            throw new NotFoundException("Course not found");

        var workout = dto.Id.HasValue
            ? await dbContext.Workouts.GetByIdOrThrowsNotFoundException(dto.Id.Value)
            : new Entities.Course.Workout()
            {
                CourseId = dto.CourseId,
                Order = await dbContext.Workouts
                    .Where(x => x.CourseId == dto.CourseId)
                    .CountAsync() + 1
            };

        workout.Title = dto.Title;
        workout.HasRest = dto.HasRest;
        
        if (dto.Order.HasValue)
            workout.Order = dto.Order.Value;

        workout = dbContext.Update(workout).Entity;
        await dbContext.SaveChangesAsync();

        return workout.Id;
    }

    public async Task Remove(long id)
    {
        var workout = await dbContext.Workouts.GetByIdOrThrowsNotFoundException(id);

        dbContext.Remove(workout);
        await dbContext.SaveChangesAsync();
    }

    #region Exercise

    public async Task<Wrapper> GetAllExercises(long userId, long workoutId, DataQueryRequest query)
    {
        return await dbContext
            .Exercises
            .Where(x => x.WorkoutId == workoutId)
            .Select(x => new GetExerciseDto
            {
                Id = x.Id, WorkoutId = x.WorkoutId, Title = x.Title,
                Description = x.Description,
                Assets = x.Assets,
                Duration = x.Duration,
                IsDone = dbContext.CourseItemStates.Any(sh =>
                    sh.EntityId == sh.Id && sh.UserId == userId && sh.Type == EnumHistoryEntityType.Exercise),
                Order = x.Order
            })
            .OrderBy(x => x.Order)
            .GetByDataQueryAsync(query);
    }

    public async Task<long> CrateOrUpdateExercise(CreateOrUpdateExerciseDto dto)
    {
        await dbContext.Workouts.ExistsOrThrowsNotFoundException(dto.WorkoutId);

        var exercise = dto.Id.HasValue
            ? await dbContext.Exercises.GetByIdOrThrowsNotFoundException(dto.Id.Value)
            : new Exercise();

        exercise.WorkoutId = dto.WorkoutId;
        exercise.Title = dto.Title;
        exercise.Description = dto.Description;
        exercise.Assets = dto.Assets;
        exercise.Duration = dto.Duration;

        if (dto.Order.HasValue)
            exercise.Order = dto.Order.Value;

        await dbContext.Transactional(async () =>
        {
            exercise = dbContext.Update(exercise).Entity;
            await dbContext.SaveChangesAsync();

            await dbContext.ExerciseMetrics.Where(x => x.ExerciseId == exercise.Id).ExecuteDeleteAsync();

            dbContext.ExerciseMetrics.AddRange(dto.Metrics.Select(x => new ExerciseMetric()
            {
                ExerciseId = exercise.Id,
                Metric = x.Metric,
                Value = x.Value,
            }));

            await dbContext.SaveChangesAsync();
        });

        return exercise.Id;
    }

    public async Task ResetWorkout(long userId, long workoutId)
    {
        var ids = await dbContext.Exercises.Where(x => x.WorkoutId == workoutId)
            .Select(x => x.Id).ToListAsync();

        ids.Add(workoutId);

        await dbContext.CourseItemStates
            .Where(x => x.UserId == userId && ids.Contains(x.EntityId) &&
                        (x.Type == EnumHistoryEntityType.Workout || x.Type == EnumHistoryEntityType.Exercise))
            .ExecuteDeleteAsync();
    }

    public async Task RemoveExercise(long id)
    {
        var exercise = dbContext.Exercises.GetByIdOrThrowsNotFoundException(id);

        dbContext.Remove(exercise);
        await dbContext.SaveChangesAsync();
    }

    #endregion
}