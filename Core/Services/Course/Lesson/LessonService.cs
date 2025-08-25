using BRB.Core.Common.Exceptions;
using BRB.Core.Common.Models;
using BRB.Core.EF.Attributes;
using BRB.Core.EF.Extensions;
using Core.Brokers.DbContext;
using Core.Enums;
using Core.Services.Course.Lesson.Contracts;
using ResultWrapper.Library;

namespace Core.Services.Course.Lesson;

[Injectable]
public class LessonService(AppDbContext dbContext)
{
    public async Task<Wrapper> GetAll(DataQueryRequest query, long? courseId = null)
    {
        var q = dbContext.Lessons.AsQueryable();
        if (courseId is not null)
            q = q.Where(x => x.CourseId == courseId);

        return await q
            .Select(x => new
            {
                x.Id,
                x.CourseId,
                x.Duration,
                x.IsFree,
                x.Title,
                x.Description
            })
            .GetByDataQueryAsync(query);
    }

    public async Task<long> CrateOrUpdate(CreateOrUpdateLessonDto dto)
    {
        if (!dbContext.Courses.Any(x => x.Id == dto.CourseId && x.Type == EnumCourseType.Lesson))
            throw new NotFoundException("Course not found");
        
        var lesson = dto.Id.HasValue
            ? await dbContext.Lessons.GetByIdOrThrowsNotFoundException(dto.Id.Value)
            : new Entities.Course.Lesson();

        lesson.CourseId = dto.CourseId;
        lesson.IsFree = dto.IsFree;
        lesson.Duration = dto.Duration;
        lesson.Assets = dto.Assets;
        lesson.Title = dto.Title;
        lesson.Description = dto.Description;

        lesson = dbContext.Update(lesson).Entity;
        await dbContext.SaveChangesAsync();
        
        return lesson.Id;
    }

    public async Task Remove(long id)
    {
        var lesson = await dbContext.Lessons.GetByIdOrThrowsNotFoundException(id);

        dbContext.Remove(lesson);
        await dbContext.SaveChangesAsync();
    }
}