using BRB.Core.Common.Exceptions;
using BRB.Core.EF.Attributes;
using Core.Brokers.DbContext;
using Microsoft.EntityFrameworkCore;

namespace Core.Services;

[Injectable]
public class UserService(AppDbContext context)
{
    public async Task<object> GetUserAsync(long userId)
    {
        var user = await context.Users
            .Select(x => new
            {
                x.Id,
                x.Email,
                x.Roles
            })
            .FirstOrDefaultAsync(x => x.Id == userId)
            ?? throw new NotFoundException("User not found");

        var extra = await context.UserExtras
            .Select(x => new
            {
                x.UserId,
                x.Weight,
                x.Height,
                x.Bmi,
                x.Gender,
                x.BirthDate,
                x.Photo,
                x.Name
            })
            .FirstOrDefaultAsync(x => x.UserId == userId);

        var norms = await context.UserNorms
            .Where(x => x.UserId == userId)
            .Select(x => new
            {
                x.Metric,
                x.Value
            })
            .ToListAsync();

        var dailyRecords = await context.UserDailies
            .Where(x => x.UserId == userId)
            .Select(x => new
            {
                x.Date,
                x.Metric,
                x.Value
            })
            .ToListAsync();

        return new
        {
            User = user,
            Extra = extra,
            Norms = norms,
            Dailies = dailyRecords
        };
    }
}