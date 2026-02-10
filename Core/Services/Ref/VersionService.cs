using BRB.Core.Common.Exceptions;
using BRB.Core.Common.Extensions;
using BRB.Core.Common.Models;
using BRB.Core.EF.Attributes;
using BRB.Core.EF.Extensions;
using Core.Brokers.DbContext;
using Core.Services.Ref.Contracts;
using Microsoft.EntityFrameworkCore;
using ResultWrapper.Library;
using Version = Core.Entities.Refs.Version;

namespace Core.Services.Ref;

[Injectable]
public class VersionService(AppDbContext dbContext)
{
    public async Task<Wrapper> GetAll(DataQueryRequest q)
    {
        return await dbContext.Versions.GetByDataQueryAsync(q);
    }

    public async Task<Version> GetLatestVersion()
    {
        return await dbContext.Versions
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(x => x.IsActive) ?? throw new NotFoundException("No active version");
    }

    public async Task<CheckDto> Check(string version)
    {
        var entity =
            await dbContext.Versions.FirstOrDefaultAsync(x =>
                x.Key == version.ToLowerInvariant()) ??
            throw new NotFoundException("Version not found");

        return new CheckDto()
        {
            Id = entity.Id,
            IsActive = entity.IsActive,
            Version = entity.Key
        };
    }

    public async Task CreateOrUpdate(CreateOrUpdateVersionDto dto)
    {
        if (dto.Id.HasValue)
        {
            //updating
            var version = await dbContext.Versions.GetByIdAsync(dto.Id!.Value) ??
                          throw new NotFoundException("Version not found");

            version.IsActive = dto.IsActive;

            await dbContext.SaveChangesAsync();
        }
        else if (!dto.Key.IsNullOrEmpty())
        {
            //creating
            if (await dbContext.Versions
                    .AnyAsync(x => x.Key == dto.Key!.ToLowerInvariant()))
                throw new BadRequestException("Version already exists");

            dbContext.Versions.Add(new Version()
            {
                Key = dto.Key!.ToLowerInvariant(),
                IsActive = false
            });
            await dbContext.SaveChangesAsync();
        }
        else throw new BadRequestException("Id or version key must be specified");
    }
}