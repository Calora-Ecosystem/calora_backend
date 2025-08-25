using BRB.Core.Common.Models;
using BRB.Core.EF.Attributes;
using BRB.Core.EF.Extensions;
using Core.Brokers.DbContext;
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

    public async Task CrateOrUpdate(CreateOrUpdateLessonDto dto)
    {
        var lesson = dto.Id.HasValue
            ? await dbContext.Lessons.GetByIdOrThrowsNotFoundException(dto.Id.Value)
            : new Entities.Course.Lesson();

        lesson.CourseId = dto.CourseId;
        lesson.IsFree = dto.IsFree;
        lesson.Duration = dto.Duration;
        lesson.Assets = dto.Assets;
        lesson.Title = dto.Title;
        lesson.Description = dto.Description;

        dbContext.Update(lesson);
        await dbContext.SaveChangesAsync();
    }

    public async Task Remove(long id)
    {
        var lesson = await dbContext.Lessons.GetByIdOrThrowsNotFoundException(id);

        dbContext.Remove(lesson);
        await dbContext.SaveChangesAsync();
    }
}