using BRB.Core.EF.Attributes;
using Core.Brokers.DbContext;
using Core.Entities.Groups;
using Core.Enums;
using Core.Helpers;
using Core.Services.StepGroups.Contracts;
using Core.Services.StepGroups.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Core.Services.StepGroups;

/// <summary>
/// Qadam guruhlari: userlar guruh yaratadi, do'stlarini taklif kodi orqali qo'shadi
/// va guruh ichida qadamlar bo'yicha bellashadi. Qadamlar <c>user_dailies</c>
/// (<see cref="EnumMetrics.Step"/>) dan olinadi — alohida saqlanmaydi.
/// </summary>
[Injectable]
public class StepGroupService(AppDbContext dbContext)
{
    public const int MaxMembers = 50;
    public const int MaxGroupsPerUser = 20;
    private const string CodePrefix = "CAL-";

    public async Task<List<StepGroupDto>> GetMy(long userId, DateTime? from, DateTime? to)
    {
        var (start, end) = NormalizePeriod(from, to);

        return await dbContext.StepGroups
            .AsNoTracking()
            .Where(g => g.Members.Any(m => m.UserId == userId))
            .OrderByDescending(g => g.CreatedAt)
            .Select(g => new StepGroupDto
            {
                Id = g.Id,
                Name = g.Name,
                InviteCode = g.InviteCode,
                OwnerId = g.OwnerId,
                IsOwner = g.OwnerId == userId,
                MemberCount = g.Members.Count,
                TotalSteps = dbContext.UserDailies
                    .Where(d => d.Metric == EnumMetrics.Step && d.Date >= start && d.Date <= end &&
                                g.Members.Any(m => m.UserId == d.UserId))
                    .Sum(d => d.Value),
                CreatedAt = g.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<StepGroupDetailDto> GetById(long userId, long groupId, DateTime? from, DateTime? to)
    {
        var (start, end) = NormalizePeriod(from, to);

        var group = await dbContext.StepGroups
                        .AsNoTracking()
                        .Where(g => g.Id == groupId && g.Members.Any(m => m.UserId == userId))
                        .Select(g => new { g.Id, g.Name, g.InviteCode, g.OwnerId, g.CreatedAt })
                        .FirstOrDefaultAsync()
                    ?? throw new StepGroupNotFoundException();

        var members = await dbContext.StepGroupMembers
            .AsNoTracking()
            .Where(m => m.GroupId == groupId)
            .Select(m => new StepGroupMemberDto
            {
                UserId = m.UserId,
                Name = m.User.Name,
                Photo = dbContext.UserExtras.Where(e => e.UserId == m.UserId).Select(e => e.Photo).FirstOrDefault(),
                Steps = dbContext.UserDailies
                    .Where(d => d.UserId == m.UserId && d.Metric == EnumMetrics.Step &&
                                d.Date >= start && d.Date <= end)
                    .Sum(d => d.Value),
                IsMe = m.UserId == userId,
                IsOwner = m.UserId == group.OwnerId,
                JoinedAt = m.CreatedAt
            })
            .ToListAsync();

        var ranked = members
            .OrderByDescending(m => m.Steps)
            .ThenBy(m => m.JoinedAt)
            .Select((m, i) =>
            {
                m.Index = i + 1;
                return m;
            })
            .ToList();

        return new StepGroupDetailDto
        {
            Id = group.Id,
            Name = group.Name,
            InviteCode = group.InviteCode,
            OwnerId = group.OwnerId,
            IsOwner = group.OwnerId == userId,
            MemberCount = ranked.Count,
            TotalSteps = ranked.Sum(m => m.Steps),
            CreatedAt = group.CreatedAt,
            Members = ranked
        };
    }

    public async Task<StepGroupDetailDto> Create(long userId, CreateStepGroupDto dto)
    {
        await EnsureGroupLimit(userId);

        var group = new Entities.Groups.StepGroup
        {
            Name = dto.Name.Trim(),
            InviteCode = await GenerateUniqueCode(),
            OwnerId = userId,
            Members = [new StepGroupMember { UserId = userId }]
        };

        dbContext.StepGroups.Add(group);
        await dbContext.SaveChangesAsync();

        return await GetById(userId, group.Id, null, null);
    }

    /// <summary>Taklif kodi orqali qo'shilish. Allaqachon a'zo bo'lsa guruhni qaytaradi.</summary>
    public async Task<StepGroupDetailDto> Join(long userId, JoinStepGroupDto dto)
    {
        var code = dto.Code.Trim().ToUpperInvariant();

        var group = await dbContext.StepGroups
                        .AsNoTracking()
                        .Where(g => g.InviteCode == code)
                        .Select(g => new
                        {
                            g.Id,
                            MemberCount = g.Members.Count,
                            IsMember = g.Members.Any(m => m.UserId == userId)
                        })
                        .FirstOrDefaultAsync()
                    ?? throw new StepGroupNotFoundException();

        if (!group.IsMember)
        {
            if (group.MemberCount >= MaxMembers)
                throw new StepGroupFullException();

            await EnsureGroupLimit(userId);

            try
            {
                dbContext.StepGroupMembers.Add(new StepGroupMember { GroupId = group.Id, UserId = userId });
                await dbContext.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                // (GroupId, UserId) unique — parallel so'rov allaqachon qo'shgan.
                dbContext.ChangeTracker.Clear();
            }
        }

        return await GetById(userId, group.Id, null, null);
    }

    /// <summary>Faqat admin (egasi) guruhni butunlay o'chiradi.</summary>
    public async Task Delete(long userId, long groupId)
    {
        var group = await dbContext.StepGroups.FirstOrDefaultAsync(g => g.Id == groupId)
                    ?? throw new StepGroupNotFoundException();

        if (group.OwnerId != userId)
            throw new StepGroupOwnerOnlyException();

        await dbContext.Transactional(async () =>
        {
            await dbContext.StepGroupMembers.Where(m => m.GroupId == groupId).ExecuteDeleteAsync();
            dbContext.StepGroups.Remove(group);
            await dbContext.SaveChangesAsync();
        });
    }

    /// <summary>
    /// Guruhdan chiqish. Admin chiqsa adminlik eng eski a'zoga o'tadi;
    /// oxirgi a'zo chiqsa guruh o'chiriladi.
    /// </summary>
    public async Task Leave(long userId, long groupId)
    {
        var group = await dbContext.StepGroups.FirstOrDefaultAsync(g => g.Id == groupId)
                    ?? throw new StepGroupNotFoundException();

        var membership = await dbContext.StepGroupMembers
                             .FirstOrDefaultAsync(m => m.GroupId == groupId && m.UserId == userId)
                         ?? throw new StepGroupNotFoundException();

        await dbContext.Transactional(async () =>
        {
            dbContext.StepGroupMembers.Remove(membership);

            if (group.OwnerId == userId)
            {
                var nextOwnerId = await dbContext.StepGroupMembers
                    .Where(m => m.GroupId == groupId && m.UserId != userId)
                    .OrderBy(m => m.CreatedAt)
                    .Select(m => (long?)m.UserId)
                    .FirstOrDefaultAsync();

                if (nextOwnerId is null)
                    dbContext.StepGroups.Remove(group);
                else
                    group.OwnerId = nextOwnerId.Value;
            }

            await dbContext.SaveChangesAsync();
        });
    }

    /// <summary>Admin a'zoni guruhdan chiqaradi.</summary>
    public async Task RemoveMember(long userId, long groupId, long memberUserId)
    {
        var ownerId = await dbContext.StepGroups
                          .Where(g => g.Id == groupId)
                          .Select(g => (long?)g.OwnerId)
                          .FirstOrDefaultAsync()
                      ?? throw new StepGroupNotFoundException();

        if (ownerId != userId)
            throw new StepGroupOwnerOnlyException();

        if (memberUserId == ownerId)
            throw new StepGroupCannotRemoveOwnerException();

        var deleted = await dbContext.StepGroupMembers
            .Where(m => m.GroupId == groupId && m.UserId == memberUserId)
            .ExecuteDeleteAsync();

        if (deleted == 0)
            throw new StepGroupMemberNotFoundException();
    }

    private async Task EnsureGroupLimit(long userId)
    {
        if (await dbContext.StepGroupMembers.CountAsync(m => m.UserId == userId) >= MaxGroupsPerUser)
            throw new StepGroupLimitReachedException();
    }

    private async Task<string> GenerateUniqueCode()
    {
        while (true)
        {
            var code = CodeGenerator.Generate(CodePrefix, 5);
            if (!await dbContext.StepGroups.AnyAsync(g => g.InviteCode == code))
                return code;
        }
    }

    /// <summary>Default — bugun (<c>users/steps/stat</c> bilan bir xil).</summary>
    private static (DateTime, DateTime) NormalizePeriod(DateTime? from, DateTime? to) =>
        (from ?? DateTime.Now.Date, to ?? DateTime.Now.Date.AddDays(1));
}
