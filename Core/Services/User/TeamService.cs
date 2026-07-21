using BRB.Core.EF.Attributes;
using Core.Brokers.DbContext;
using Core.Entities.Crm;
using Core.Entities.Crm.Enum;
using Core.Enums;
using Core.Exceptions;
using Core.Services.Auth;
using Core.Services.User.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Core.Services.User;

/// <summary>
/// Jamoa (xodimlar) boshqaruvi — faqat SuperAdmin uchun. "Jamoa a'zosi" deb
/// SuperAdmin, HeadOfSales yoki Operator rollaridan kamida bittasiga ega
/// foydalanuvchiga aytiladi (oddiy "User" rolidagilar bu ro'yxatga kirmaydi).
/// </summary>
[Injectable]
public class TeamService(AppDbContext context, AuthService authService)
{
    private static readonly string[] StaffRoles =
        [nameof(EnumRole.SuperAdmin), nameof(EnumRole.HeadOfSales), nameof(EnumRole.Operator)];

    private const string OperatorRole = nameof(EnumRole.Operator);
    private const string SuperAdminRole = nameof(EnumRole.SuperAdmin);

    // jsonb `@>` — mavjud CRM servislaridagi uslub bilan bir xil.
    private IQueryable<Entities.Auth.User> StaffUsers() =>
        context.Users.FromSqlRaw(
            """
            SELECT * FROM users
            WHERE roles @> '["SuperAdmin"]'::jsonb
               OR roles @> '["HeadOfSales"]'::jsonb
               OR roles @> '["Operator"]'::jsonb
            """);

    public async Task<IEnumerable<TeamMemberDto>> GetAllAsync()
    {
        var members = await StaffUsers()
            .OrderBy(u => u.Id)
            .Select(u => new TeamMemberDto
            {
                Id = u.Id,
                Name = u.Name,
                Email = u.Email,
                Phone = u.Phone,
                Roles = u.Roles,
                CreatedAt = u.CreatedAt
            })
            .ToListAsync();

        var ids = members.Select(m => m.Id).ToList();
        var load = await context.Leads
            .Where(l => l.OperatorId != null && ids.Contains(l.OperatorId.Value)
                        && l.Status != EnumLeadStatus.Won && l.Status != EnumLeadStatus.Lost)
            .GroupBy(l => l.OperatorId!.Value)
            .Select(g => new { Id = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Id, x => x.Count);

        foreach (var m in members) m.ActiveLeads = load.GetValueOrDefault(m.Id, 0);

        return members;
    }

    /// <summary>
    /// Jamoaga a'zo qo'shadi. Email bo'yicha foydalanuvchi mavjud bo'lsa — unga rollar
    /// biriktiriladi, aks holda yangi akkaunt yaratiladi (kirish email + OTP orqali).
    /// </summary>
    public async Task<long> CreateAsync(CreateTeamMemberDto dto)
    {
        var roles = NormalizeRoles(dto.Roles);
        var email = dto.Email.Trim();

        var user = await context.Users.FirstOrDefaultAsync(u => u.Email != null && EF.Functions.ILike(u.Email, email));

        if (user is null)
        {
            user = context.Add(new Entities.Auth.User
            {
                Name = dto.Name.Trim(),
                Email = email,
                Phone = dto.Phone,
                Roles = roles
            }).Entity;
        }
        else
        {
            if (user.Roles.Intersect(StaffRoles).Any())
                throw new AlreadyExistsException("team_member_already_exists");

            user.Name = dto.Name.Trim();
            if (!string.IsNullOrWhiteSpace(dto.Phone)) user.Phone = dto.Phone;
            // Mavjud rollarni (masalan "User") saqlab qolamiz.
            user.Roles = user.Roles.Concat(roles).Distinct().ToList();
        }

        await context.SaveChangesAsync();
        return user.Id;
    }

    public async Task UpdateAsync(long actorId, long memberId, UpdateTeamMemberDto dto)
    {
        var roles = NormalizeRoles(dto.Roles);
        var user = await GetStaffOrThrowAsync(memberId);

        // O'zidan SuperAdmin rolini olib tashlash — tizimdan qulflanib qolishga olib keladi.
        if (actorId == memberId && !roles.Contains(SuperAdminRole))
            throw new BadRequestException("cannot_revoke_own_admin_role");

        await EnsureNotLastAdminAsync(user, roles);

        var wasOperator = user.Roles.Contains(OperatorRole);

        user.Name = dto.Name.Trim();
        user.Phone = dto.Phone;
        // Jamoa bo'lmagan rollarni (User) tegmasdan qoldiramiz.
        user.Roles = user.Roles.Except(StaffRoles).Concat(roles).Distinct().ToList();

        if (wasOperator && !roles.Contains(OperatorRole))
            await ReleaseLeadsAsync(memberId);

        await context.SaveChangesAsync();
        await authService.KillAllUserSessions(user.Id);
    }

    /// <summary>
    /// Jamoadan chiqaradi: barcha xodim rollari olib tashlanadi (akkaunt oddiy foydalanuvchi
    /// sifatida qoladi), operator bo'lsa leadlari qayta taqsimlanadi.
    /// </summary>
    public async Task DeleteAsync(long actorId, long memberId)
    {
        if (actorId == memberId)
            throw new BadRequestException("cannot_remove_self");

        var user = await GetStaffOrThrowAsync(memberId);
        await EnsureNotLastAdminAsync(user, []);

        if (user.Roles.Contains(OperatorRole))
            await ReleaseLeadsAsync(memberId);

        user.Roles = user.Roles.Except(StaffRoles).DefaultIfEmpty(nameof(EnumRole.User)).ToList();

        await context.SaveChangesAsync();
        await authService.KillAllUserSessions(user.Id);
    }

    private async Task<Entities.Auth.User> GetStaffOrThrowAsync(long memberId)
    {
        var user = await context.Users.FirstOrDefaultAsync(u => u.Id == memberId)
                   ?? throw new NotFoundException("team_member_not_found");

        if (!user.Roles.Intersect(StaffRoles).Any())
            throw new NotFoundException("team_member_not_found");

        return user;
    }

    /// <summary>Oxirgi SuperAdmin roldan mahrum bo'lib qolmasligini kafolatlaydi.</summary>
    private async Task EnsureNotLastAdminAsync(Entities.Auth.User user, List<string> nextRoles)
    {
        if (!user.Roles.Contains(SuperAdminRole) || nextRoles.Contains(SuperAdminRole)) return;

        var admins = await context.Users
            .FromSqlRaw("""SELECT * FROM users WHERE roles @> '["SuperAdmin"]'::jsonb""")
            .CountAsync();

        if (admins <= 1) throw new BadRequestException("cannot_remove_last_admin");
    }

    /// <summary>
    /// Operatorning yakunlanmagan leadlarini qolgan operatorlarga eng kam yuklanganidan
    /// boshlab taqsimlaydi; operator qolmasa — "Yangi" ustuniga qaytaradi.
    /// </summary>
    private async Task ReleaseLeadsAsync(long operatorId)
    {
        var activeLeads = await context.Leads
            .Where(l => l.OperatorId == operatorId
                        && l.Status != EnumLeadStatus.Won && l.Status != EnumLeadStatus.Lost)
            .ToListAsync();

        if (activeLeads.Count == 0) return;

        var others = (await context.Users
                .FromSqlRaw("""SELECT * FROM users WHERE roles @> '["Operator"]'::jsonb""")
                .Select(u => u.Id)
                .ToListAsync())
            .Where(id => id != operatorId)
            .ToList();

        if (others.Count == 0)
        {
            foreach (var lead in activeLeads)
            {
                lead.OperatorId = null;
                lead.Status = EnumLeadStatus.New;
            }

            return;
        }

        var load = await context.Leads
            .Where(l => l.OperatorId != null && others.Contains(l.OperatorId.Value)
                        && l.Status != EnumLeadStatus.Won && l.Status != EnumLeadStatus.Lost)
            .GroupBy(l => l.OperatorId!.Value)
            .Select(g => new { Id = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Id, x => x.Count);

        foreach (var lead in activeLeads)
        {
            var target = others.OrderBy(id => load.GetValueOrDefault(id, 0)).ThenBy(id => id).First();
            lead.OperatorId = target;
            load[target] = load.GetValueOrDefault(target, 0) + 1;

            context.Add(new LeadActivity
            {
                LeadId = lead.Id,
                Type = EnumLeadActivityType.Assigned,
                Description = "Boshqa operatorga qayta biriktirildi",
                ActorId = target
            });
        }
    }

    private static List<string> NormalizeRoles(EnumRole[] roles)
    {
        var staff = roles.Select(r => r.ToString()).Intersect(StaffRoles).Distinct().ToList();
        if (staff.Count == 0) throw new BadRequestException("at_least_one_team_role_required");
        return staff;
    }
}
