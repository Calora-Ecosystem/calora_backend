using BRB.Core.EF.Attributes;
using BRB.Core.EF.Extensions;
using Core.Brokers.DbContext;
using Core.Enums;
using Core.Services.Course.Course.Contracts;
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

    public async Task CreateOrUpdate(CreateOrUpdateCourseDto dto)
    {
        var course = dto.Id.HasValue
            ? await context.Courses.GetByIdOrThrowsNotFoundException(dto.Id.Value)
            : new Entities.Course.Course();

        course.Type = dto.Type;
        course.Gender = dto.Gender;
        course.Title = dto.Title;
        course.Description = dto.Description;
        course.Price = dto.Price;

        context.Update(course);
        await context.SaveChangesAsync();
    }

    public async Task Remove(long id)
    {
        var course = await context.Courses.GetByIdOrThrowsNotFoundException(id);

        context.Courses.Remove(course);
        await context.SaveChangesAsync();
    }
}