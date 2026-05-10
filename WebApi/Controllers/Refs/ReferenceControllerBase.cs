using BRB.Core.Common.Models;
using BRB.Core.Common.Models.Base;
using BRB.Core.EF.Extensions;
using Core.Attributes;
using Core.Brokers.DbContext;
using Core.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ResultWrapper.Library;

namespace WebApi.Controllers.Refs;

[ApiExplorerSettings(GroupName = "References")]
[RoleAuthorize(EnumRole.SuperAdmin)]
public abstract class ReferenceControllerBase<T>(AppDbContext dbContext)
    : ControllerBase where T : ReferenceModelBase<long>
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<WrapperGeneric<IEnumerable<T>>> GetAll([FromQuery] DataQueryRequest q)
    {
        var query = dbContext.Set<T>()
            .FilterByExpressions(q.FilteringExpression);

        var total = await query.CountAsync();

        var items = await dbContext.Set<T>()
            .Sort(q)
            .FilterByExpressions(q.FilteringExpression)
            .Page(q)
            .ToListAsync();

        var result = WrapperGeneric<IEnumerable<T>>.ResultFromContent(items);

        result.Total = total;

        return result;
    }

    [HttpPost]
    public async Task<T> CreateOrUpdate(T data)
    {
        var storedEntity = await dbContext.Set<T>()
            .GetByIdAsync(data.Id);

        if (storedEntity is null)
        {
            storedEntity = dbContext.Set<T>().Add(data).Entity;
        }
        else
        {
            foreach (var propertyInfo in typeof(T).GetProperties())
            {
                if (propertyInfo.Name.Contains("Id", StringComparison.OrdinalIgnoreCase))
                    continue;

                var value = propertyInfo.GetValue(data);
                storedEntity?.GetType()?.GetProperty(propertyInfo.Name)?.SetValue(storedEntity, value);
            }

            storedEntity = dbContext.Set<T>().Update(storedEntity!).Entity;
        }

        await dbContext.SaveChangesAsync();

        return storedEntity;
    }

    [HttpDelete]
    public async Task<T> Remove(long id)
    {
        var entity = await dbContext.Set<T>().GetByIdOrThrowsNotFoundException(id);
        dbContext.Set<T>().Remove(entity);

        await dbContext.SaveChangesAsync();

        return entity;
    }
}