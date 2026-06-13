using BRB.Core.EF.Attributes;
using Core.Brokers.DbContext;
using Core.Entities.Crm.Enum;
using Core.Enums;
using Core.Exceptions;
using Core.Services.Crm.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Core.Services.Crm;

[Injectable]
public class CrmOperatorService(AppDbContext context)
{
    private const string OperatorRole = nameof(EnumRole.Operator);

    private IQueryable<Core.Entities.Auth.User> OperatorUsers() =>
        context.Users.FromSqlInterpolated($"SELECT * FROM users WHERE roles @> '[\"Operator\"]'::jsonb");

    public async Task<GetMeDto> GetMeAsync(long userId)
    {
        return await context.Users
                   .Where(u => u.Id == userId)
                   .Select(u => new GetMeDto { Id = u.Id, Name = u.Name, Roles = u.Roles })
                   .FirstOrDefaultAsync()
               ?? throw new NotFoundException("user_not_found");
    }

    /// <summary>
    /// Creates an operator. If a user with the e-mail already exists it is promoted to Operator,
    /// otherwise a new account is created (signs in via e-mail OTP).
    /// </summary>
    public async Task<long> CreateOperatorAsync(CreateOperatorDto dto)
    {
        var email = dto.Email.Trim();

        var user = await context.Users.FirstOrDefaultAsync(u => u.Email != null && EF.Functions.ILike(u.Email, email));

        if (user is null)
        {
            user = context.Add(new Core.Entities.Auth.User
            {
                Name = dto.Name.Trim(),
                Email = email,
                Phone = dto.Phone,
                Roles = [OperatorRole]
            }).Entity;
        }
        else
        {
            if (user.Roles.Contains(OperatorRole))
                throw new AlreadyExistsException("operator_already_exists");

            user.Name = dto.Name.Trim();
            if (!string.IsNullOrWhiteSpace(dto.Phone)) user.Phone = dto.Phone;
            user.Roles = user.Roles.Append(OperatorRole).Distinct().ToList();
        }

        await context.SaveChangesAsync();
        return user.Id;
    }

    /// <summary>
    /// Removes the operator role. Their active (non Won/Lost) leads are re-distributed across the
    /// remaining operators (least-loaded) so no lead is dropped; if none remain they go back to the
    /// unassigned "New" column for the Head of Sales to handle.
    /// </summary>
    public async Task DeleteOperatorAsync(long operatorId)
    {
        var user = await context.Users.FirstOrDefaultAsync(u => u.Id == operatorId)
                   ?? throw new NotFoundException("operator_not_found");

        if (!user.Roles.Contains(OperatorRole))
            throw new NotFoundException("operator_not_found");

        // Remaining operators (excluding the one being removed).
        var others = (await OperatorUsers().Select(u => u.Id).ToListAsync())
            .Where(id => id != operatorId)
            .ToList();

        var activeLeads = await context.Leads
            .Where(l => l.OperatorId == operatorId
                        && l.Status != EnumLeadStatus.Won && l.Status != EnumLeadStatus.Lost)
            .ToListAsync();

        if (others.Count > 0)
        {
            // Seed round-robin from current load so distribution stays balanced.
            var load = await context.Leads
                .Where(l => others.Contains(l.OperatorId!.Value)
                            && l.Status != EnumLeadStatus.Won && l.Status != EnumLeadStatus.Lost)
                .GroupBy(l => l.OperatorId!.Value)
                .Select(g => new { Id = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Id, x => x.Count);

            foreach (var lead in activeLeads)
            {
                var target = others.OrderBy(id => load.GetValueOrDefault(id, 0)).ThenBy(id => id).First();
                lead.OperatorId = target;
                load[target] = load.GetValueOrDefault(target, 0) + 1;

                context.Add(new Entities.Crm.LeadActivity
                {
                    LeadId = lead.Id,
                    Type = EnumLeadActivityType.Assigned,
                    Description = "Boshqa operatorga qayta biriktirildi",
                    ActorId = target
                });
            }
        }
        else
        {
            foreach (var lead in activeLeads)
            {
                lead.OperatorId = null;
                lead.Status = EnumLeadStatus.New;
            }
        }

        user.Roles = user.Roles.Where(r => r != OperatorRole).ToList();
        await context.SaveChangesAsync();
    }
}
