using BRB.Core.EF.Attributes;
using BRB.Core.EF.Extensions;
using Core.Brokers.DbContext;
using Core.Entities.Course;
using Core.Entities.Course.Enum;
using Core.Enums;
using Core.Services.Course.Course.Contracts;
using Microsoft.EntityFrameworkCore;
using ResultWrapper.Library;

namespace Core.Services.Course.Course;

[Injectable]
public class CourseService(AppDbContext context)
{
    public async Task<Wrapper> GetAll(GetCourseQueryRequest query)
    {
        return await context.Courses
            .Where(x => x.Gender == query.Gender || x.Gender == null)
            .Select(x => new
            {
                x.Id,
                x.Title,
                x.Description,
                x.Gender,
                x.Type,
                Total = x.Type == EnumCourseType.Lesson ? x.Lessons.Count() : x.Workouts.Count(),
            })
            .GetByDataQueryAsync(query);
    }

    public async Task<long> CreateOrUpdate(CreateOrUpdateCourseDto dto)
    {
        var course = dto.Id.HasValue
            ? await context.Courses.GetByIdOrThrowsNotFoundException(dto.Id.Value)
            : new Entities.Course.Course();

        course.Type = dto.Type;
        course.Gender = dto.Gender;
        course.Title = dto.Title;
        course.Description = dto.Description;
        course.Price = dto.Price;
        course.Assets = dto.Assets;

        course = context.Update(course).Entity;
        await context.SaveChangesAsync();

        return course.Id;
    }

    public async Task Remove(long id)
    {
        var course = await context.Courses.GetByIdOrThrowsNotFoundException(id);

        context.Courses.Remove(course);
        await context.SaveChangesAsync();
    }


    public async Task FinishEntity(long userId, long entityId, EnumHistoryEntityType type)
    {
        switch (type)
        {
            case EnumHistoryEntityType.Lesson:
                await context.Lessons.ExistsOrThrowsNotFoundException(entityId);
                break;

            case EnumHistoryEntityType.Workout:
                await context.Workouts.ExistsOrThrowsNotFoundException(entityId);
                break;

            case EnumHistoryEntityType.Exercise:
                await context.Exercises.ExistsOrThrowsNotFoundException(entityId);
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }

        var entity = await context.StepHistories.FirstOrDefaultAsync(x =>
                         x.UserId == userId && x.EntityId == entityId && x.Type == type) ??
                     new StepHistory()
                     {
                         EntityId = entityId,
                         UserId = userId,
                         Type = EnumHistoryEntityType.Exercise,
                     };

        entity.UpdatedAt = DateTime.Now;

        context.Update(entity);
        await context.SaveChangesAsync();
    }
}