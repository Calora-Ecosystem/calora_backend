using BRB.Core.Common.Exceptions;
using BRB.Core.Common.Models;
using BRB.Core.EF.Attributes;
using BRB.Core.EF.Extensions;
using Core.Brokers.DbContext;
using Core.Entities.Course;
using Core.Entities.Course.Enum;
using Core.Enums;
using Core.Services.Course.Common;
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
            .Select(x => new GetCourseDto
            {
                Id = x.Id, Title = x.Title, Description = x.Description,
                Gender = x.Gender,
                Type = x.Type,
                Total = x.Type == EnumCourseType.Lesson ? x.Lessons.Count() : x.Workouts.Count(),
                Price = x.Price,
                Order = x.Order,
                Assets = x.Assets
            })
            .OrderBy(x => x.Order)
            .GetByDataQueryAsync(query);
    }

    public async Task<long> CreateOrUpdate(CreateOrUpdateCourseDto dto)
    {
        var course = dto.Id.HasValue
            ? await context.Courses.GetByIdOrThrowsNotFoundException(dto.Id.Value)
            : new Entities.Course.Course()
            {
                Order = await context.Courses.CountAsync() + 1
            };

        course.Type = dto.Type;
        course.Gender = dto.Gender;
        course.Title = dto.Title;
        course.Description = dto.Description;
        course.Price = dto.Price;
        course.Assets = dto.Assets;

        if (dto.Order.HasValue)
            course.Order = dto.Order.Value;

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

        var entity = await context.CourseItemStates.FirstOrDefaultAsync(x =>
                         x.UserId == userId && x.EntityId == entityId && x.Type == type) ??
                     new CourseItemState()
                     {
                         EntityId = entityId,
                         UserId = userId,
                         Type = type,
                     };

        entity.UpdatedAt = DateTime.Now;

        context.Update(entity);
        await context.SaveChangesAsync();
    }

    public async Task ReorderCourseItem(EnumCourseItemType type, long id, long? beforeImteId, long? afterItemId)
    {
        Func<long, long?, long?, Task> orderFunc = type switch
        {
            EnumCourseItemType.Course => ReorderItemAsync<Entities.Course.Course>,
            EnumCourseItemType.Exercise => ReorderItemAsync<Entities.Course.Exercise>,
            EnumCourseItemType.Lesson => ReorderItemAsync<Entities.Course.Lesson>,
            EnumCourseItemType.Workout => ReorderItemAsync<Entities.Course.Workout>,
            _ => throw new Exception("Unknown type")
        };

        await orderFunc(id, beforeImteId, afterItemId);
    }

    public async Task ReorderItemAsync<T>(long itemId, long? beforeItemId, long? afterItemId) where T : BaseItem
    {
        var item = await context.Set<T>().FirstOrDefaultAsync(x => x.Id == itemId) ?? throw new NotFoundException();

        decimal newOrder;

        // oldingi va keyingi elementlarni olish
        var before = beforeItemId != null
            ? await context.Set<T>().FirstOrDefaultAsync(x => x.Id == beforeItemId) ?? throw new NotFoundException()
            : null;
        var after = afterItemId != null
            ? await context.Set<T>().FirstOrDefaultAsync(x => x.Id == afterItemId) ?? throw new NotFoundException()
            : null;

        if (before == null && after == null)
        {
            // list bo'sh yoki yagona element
            newOrder = 1.000m;
        }
        else if (before == null)
        {
            // boshiga qo'yilmoqda
            newOrder = after!.Order / 2m;
        }
        else if (after == null)
        {
            // oxiriga qo'yilmoqda
            newOrder = before!.Order + 1m;
        }
        else
        {
            // orasiga qo'yilmoqda
            newOrder = (before!.Order + after!.Order) / 2m;
        }

        item.Order = newOrder;
        await context.SaveChangesAsync();
    }
}