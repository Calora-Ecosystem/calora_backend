using BRB.Core.Common.Models;
using BRB.Core.EF.Attributes;
using BRB.Core.EF.Extensions;
using Core.Brokers.DbContext;
using Core.Entities.Billing.Enum;
using Core.Entities.Crm;
using Core.Entities.Crm.Enum;
using Core.Enums;
using Core.Services.Crm.Contracts;
using Core.Services.Crm.Enum;
using Core.Services.Crm.Exceptions;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ResultWrapper.Library;
using ForbiddenException = Core.Exceptions.ForbiddenException;

namespace Core.Services.Crm;

[Injectable]
public class LeadService(AppDbContext context, ILogger<LeadService> logger)
{
    #region Events & scoring

    [AutomaticRetry(Attempts = 3)]
    public async Task HandleEventAsync(HandleLeadEventDto dto)
    {
        var lead = await context.Leads
            .FirstOrDefaultAsync(l => l.UserId == dto.UserId && l.Priority != EnumLeadPriority.Closed
                                                             && l.Status != EnumLeadStatus.Won
                                                             && l.Status != EnumLeadStatus.Lost);

        var isNew = lead is null;
        lead ??= context.Add(new Lead { UserId = dto.UserId, Status = EnumLeadStatus.New }).Entity;

        switch (dto.Event)
        {
            case EnumLeadEvent.Registered:
                lead.IsRegistered = true;
                LogActivity(lead, EnumLeadActivityType.Registered, "Ro'yxatdan o'tdi");
                break;

            case EnumLeadEvent.SubscriptionOpened:
                lead.IsRegistered = true;
                lead.SubscriptionOpenedCount++;
                LogActivity(lead, EnumLeadActivityType.SubscriptionOpened, "Obuna sahifasini ochdi");
                break;

            case EnumLeadEvent.WorkoutStarted:
                lead.WorkoutStartedCount++;
                LogActivity(lead, EnumLeadActivityType.WorkoutStarted, "Mashg'ulotni boshladi");
                break;

            case EnumLeadEvent.WaterTracked:
                lead.WaterTrackedCount++;
                LogActivity(lead, EnumLeadActivityType.WaterTracked, "Suv tracking ishlatdi");
                break;

            case EnumLeadEvent.FoodTracked:
                lead.FoodTrackedCount++;
                LogActivity(lead, EnumLeadActivityType.FoodTracked, "Ovqat tracking ishlatdi");
                break;

            case EnumLeadEvent.AppOpened:
                lead.AppOpenCount++;
                LogActivity(lead, EnumLeadActivityType.AppOpened, "Ilovaga kirdi");
                break;

            case EnumLeadEvent.Purchased:
                await MarkWonFromPurchaseAsync(lead);
                break;

            default:
                logger.LogWarning("Unknown LeadEvent {Event} for UserId {UserId}", dto.Event, dto.UserId);
                return;
        }

        lead.LastActivity = DateTime.Now;
        lead.Score = ComputeScore(lead);
        lead.Temperature = GetTemperature(lead.Score);
        lead.Priority = GetLeadPriority(lead);

        // Persist first so a brand new lead gets its Id, then round-robin assign it.
        await context.SaveChangesAsync();

        if (isNew && lead.Status != EnumLeadStatus.Won)
            await AssignLeadAsync(lead);

        logger.LogInformation("LeadEvent {Event} applied for UserId {UserId}", dto.Event, dto.UserId);
    }

    private async Task MarkWonFromPurchaseAsync(Lead lead)
    {
        lead.Purchased = true;
        lead.Status = EnumLeadStatus.Won;
        lead.WonAt = DateTime.Now;

        var order = await context.Orders
            .Where(o => o.UserId == lead.UserId && o.Status == EnumOrderStatus.Confirmed)
            .OrderByDescending(o => o.Id)
            .Select(o => new { o.Amount, o.Provider })
            .FirstOrDefaultAsync();

        if (order is not null)
        {
            lead.WonAmount = order.Amount;
            lead.PaymentProvider = order.Provider;
        }

        var pending = await context.FollowUps.Where(f => f.LeadId == lead.Id && !f.IsDone).ToListAsync();
        foreach (var f in pending) { f.IsDone = true; f.DoneAt = DateTime.Now; }
        lead.NextFollowUpAt = null;

        LogActivity(lead, EnumLeadActivityType.Won, "Subscription sotib oldi");
    }

    /// <summary>Lead score per the CRM scoring rules.</summary>
    public int ComputeScore(Lead lead)
    {
        var score = 0;
        if (lead.IsRegistered) score += 10;

        if (lead.SubscriptionOpenedCount >= 1) score += 50;          // opened subscription page
        if (lead.SubscriptionOpenedCount > 1) score += 30;            // opened several times

        if (lead.WorkoutStartedCount >= 1) score += 15;
        if (lead.AppOpenCount >= 3) score += 20;                      // regular app usage
        if (lead.WaterTrackedCount >= 1) score += 10;
        if (lead.FoodTrackedCount >= 1) score += 10;

        return score;
    }

    public static EnumLeadTemperature GetTemperature(int score) => score switch
    {
        <= 30 => EnumLeadTemperature.Cold,
        <= 70 => EnumLeadTemperature.Warm,
        <= 100 => EnumLeadTemperature.Hot,
        _ => EnumLeadTemperature.VeryHot
    };

    public EnumLeadPriority GetLeadPriority(Lead lead)
    {
        if (lead.Purchased) return EnumLeadPriority.Closed;
        if (!lead.IsRegistered) return EnumLeadPriority.Low;
        if (lead.SubscriptionOpenedCount > 0) return EnumLeadPriority.High;
        return EnumLeadPriority.Medium;
    }

    private void LogActivity(Lead lead, EnumLeadActivityType type, string description, long? actorId = null)
    {
        context.Add(new LeadActivity
        {
            Lead = lead,
            LeadId = lead.Id,
            Type = type,
            Description = description,
            ActorId = actorId
        });
    }

    #endregion

    #region Assignment (least-loaded round-robin)

    private async Task AssignLeadAsync(Lead lead)
    {
        var operatorId = await PickOperatorAsync();
        if (operatorId is null)
        {
            logger.LogWarning("No operators available to assign Lead {LeadId}", lead.Id);
            return;
        }

        lead.OperatorId = operatorId;
        if (lead.Status == EnumLeadStatus.New)
            lead.Status = EnumLeadStatus.Assigned;

        LogActivity(lead, EnumLeadActivityType.Assigned, "Operatorga biriktirildi", operatorId);
        await context.SaveChangesAsync();
    }

    /// <summary>Returns the operator with the fewest active (non Won/Lost) leads.</summary>
    private async Task<long?> PickOperatorAsync()
    {
        // Roles is a jsonb List<string>; use Postgres containment which EF can't translate from LINQ Contains.
        const string operatorJson = "[\"Operator\"]";
        var operators = await context.Users
            .FromSqlInterpolated($"SELECT * FROM users WHERE roles @> {operatorJson}::jsonb")
            .Select(u => u.Id)
            .ToListAsync();

        if (operators.Count == 0) return null;

        var loads = await context.Leads
            .Where(l => l.OperatorId != null && l.Status != EnumLeadStatus.Won && l.Status != EnumLeadStatus.Lost)
            .GroupBy(l => l.OperatorId!.Value)
            .Select(g => new { OperatorId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.OperatorId, x => x.Count);

        return operators
            .OrderBy(id => loads.GetValueOrDefault(id, 0))
            .ThenBy(id => id)
            .First();
    }

    #endregion

    #region Status & contact

    public async Task MoveStatusAsync(long leadId, long operatorId, EnumLeadStatus status, string? reason)
    {
        var lead = await GetOwnedLeadAsync(leadId, operatorId);

        if (status == EnumLeadStatus.Lost && string.IsNullOrWhiteSpace(reason))
            throw new LostReasonRequiredException();

        var previous = lead.Status;
        lead.Status = status;
        lead.LastActivity = DateTime.Now;

        switch (status)
        {
            case EnumLeadStatus.Won:
                lead.Purchased = true;
                lead.WonAt = DateTime.Now;
                lead.Priority = EnumLeadPriority.Closed;
                LogActivity(lead, EnumLeadActivityType.Won, "Sotuv yakunlandi", operatorId);
                break;

            case EnumLeadStatus.Lost:
                lead.LostAt = DateTime.Now;
                lead.LostReason = reason;
                lead.NextFollowUpAt = null;
                LogActivity(lead, EnumLeadActivityType.Lost, $"Yo'qotildi: {reason}", operatorId);
                break;

            case EnumLeadStatus.Contacted:
                lead.LastContactedAt ??= DateTime.Now;
                LogActivity(lead, EnumLeadActivityType.StatusChanged, $"{previous} → {status}", operatorId);
                break;

            default:
                LogActivity(lead, EnumLeadActivityType.StatusChanged, $"{previous} → {status}", operatorId);
                break;
        }

        await context.SaveChangesAsync();
    }

    public async Task ContactAsync(long leadId, long operatorId)
    {
        var lead = await GetOwnedLeadAsync(leadId, operatorId);

        lead.LastContactedAt = DateTime.Now;
        lead.LastActivity = DateTime.Now;
        if (lead.Status is EnumLeadStatus.New or EnumLeadStatus.Assigned)
            lead.Status = EnumLeadStatus.Contacted;

        LogActivity(lead, EnumLeadActivityType.Contacted, "Bog'lanildi", operatorId);
        await context.SaveChangesAsync();
    }

    private async Task<Lead> GetOwnedLeadAsync(long leadId, long operatorId)
    {
        var lead = await context.Leads.FirstOrDefaultAsync(l => l.Id == leadId)
                   ?? throw new LeadNotFoundException();

        if (lead.OperatorId is null)
            lead.OperatorId = operatorId;
        else if (lead.OperatorId != operatorId)
            throw new ForbiddenException();

        return lead;
    }

    #endregion

    #region Notes

    public async Task<long> CreateOrUpdateNote(long leadId, long operatorId, UpsertNoteDto dto)
    {
        var lead = await context.Leads.FirstOrDefaultAsync(l => l.Id == leadId)
                   ?? throw new LeadNotFoundException();

        var note = dto.Id.HasValue
            ? await context.Notes.FirstOrDefaultAsync(n => n.Id == dto.Id.Value)
              ?? throw new NoteNotFoundException()
            : context.Add(new Note { LeadId = leadId, OperatorId = operatorId }).Entity;

        if (dto.Id.HasValue && note.OperatorId != operatorId)
            throw new NoteAccessDeniedException();

        note.Text = dto.Text;

        if (!dto.Id.HasValue)
            LogActivity(lead, EnumLeadActivityType.NoteAdded, "Izoh qo'shildi", operatorId);

        await context.SaveChangesAsync();
        return note.Id;
    }

    public async Task DeleteNoteAsync(long noteId, long operatorId)
    {
        var note = await context.Notes.FirstOrDefaultAsync(n => n.Id == noteId)
                   ?? throw new NoteNotFoundException();

        if (note.OperatorId != operatorId)
            throw new ForbiddenException();

        context.Notes.Remove(note);
        await context.SaveChangesAsync();
    }

    public async Task<IEnumerable<GetNoteDto>> GetNotesByLeadIdAsync(long leadId)
    {
        if (!await context.Leads.AnyAsync(l => l.Id == leadId))
            throw new LeadNotFoundException();

        return await context.Notes
            .Where(n => n.LeadId == leadId)
            .OrderByDescending(n => n.CreatedAt)
            .Select(n => new GetNoteDto
            {
                Id = n.Id,
                Text = n.Text,
                OperatorName = n.Operator.Name,
                CreatedAt = n.CreatedAt
            })
            .ToListAsync();
    }

    #endregion

    #region Follow-ups

    public async Task<long> CreateFollowUpAsync(long leadId, long operatorId, CreateFollowUpDto dto)
    {
        var lead = await GetOwnedLeadAsync(leadId, operatorId);

        var followUp = context.Add(new FollowUp
        {
            LeadId = leadId,
            OperatorId = operatorId,
            DueAt = dto.DueAt,
            Note = dto.Note
        }).Entity;

        lead.NextFollowUpAt = dto.DueAt;
        if (lead.Status is not (EnumLeadStatus.Won or EnumLeadStatus.Lost))
            lead.Status = EnumLeadStatus.FollowUp;

        LogActivity(lead, EnumLeadActivityType.FollowUpSet,
            $"Follow-up belgilandi: {dto.DueAt:dd.MM.yyyy HH:mm}", operatorId);

        await context.SaveChangesAsync();
        return followUp.Id;
    }

    public async Task CompleteFollowUpAsync(long followUpId, long operatorId)
    {
        var followUp = await context.FollowUps.FirstOrDefaultAsync(f => f.Id == followUpId)
                       ?? throw new FollowUpNotFoundException();

        if (followUp.OperatorId != operatorId)
            throw new ForbiddenException();

        followUp.IsDone = true;
        followUp.DoneAt = DateTime.Now;

        var next = await context.FollowUps
            .Where(f => f.LeadId == followUp.LeadId && !f.IsDone && f.Id != followUp.Id)
            .OrderBy(f => f.DueAt)
            .Select(f => (DateTime?)f.DueAt)
            .FirstOrDefaultAsync();

        var lead = await context.Leads.FirstAsync(l => l.Id == followUp.LeadId);
        lead.NextFollowUpAt = next;

        await context.SaveChangesAsync();
    }

    public async Task<IEnumerable<GetFollowUpDto>> GetFollowUpsAsync(long operatorId, string? scope)
    {
        var now = DateTime.Now;
        var todayEnd = now.Date.AddDays(1);

        var query = context.FollowUps.Where(f => f.OperatorId == operatorId && !f.IsDone);

        query = scope?.ToLowerInvariant() switch
        {
            "today" => query.Where(f => f.DueAt < todayEnd),
            "overdue" => query.Where(f => f.DueAt < now),
            "upcoming" => query.Where(f => f.DueAt >= todayEnd),
            _ => query
        };

        return await query
            .OrderBy(f => f.DueAt)
            .Select(f => new GetFollowUpDto
            {
                Id = f.Id,
                LeadId = f.LeadId,
                LeadName = f.Lead.User.Name,
                LeadPhone = f.Lead.User.Phone,
                DueAt = f.DueAt,
                Note = f.Note,
                IsDone = f.IsDone,
                Overdue = f.DueAt < now
            })
            .ToListAsync();
    }

    #endregion

    #region Queries

    /// <summary>
    /// Lists leads. When <paramref name="operatorScopeId"/> is set (operator role) results are
    /// restricted to that operator; HeadOfSales passes null and may filter by OperatorId.
    /// </summary>
    public async Task<Wrapper> GetAllAsync(GetLeadsQuery q, long? operatorScopeId)
    {
        var now = DateTime.Now;
        var queryable = context.Leads.AsQueryable();

        if (operatorScopeId.HasValue)
            queryable = queryable.Where(l => l.OperatorId == operatorScopeId.Value);
        else if (q.OperatorId.HasValue)
            queryable = queryable.Where(l => l.OperatorId == q.OperatorId.Value);

        if (q.Priority.HasValue) queryable = queryable.Where(l => l.Priority == q.Priority.Value);
        if (q.Status.HasValue) queryable = queryable.Where(l => l.Status == q.Status.Value);
        if (q.Temperature.HasValue) queryable = queryable.Where(l => l.Temperature == q.Temperature.Value);
        if (q.MinScore.HasValue) queryable = queryable.Where(l => l.Score >= q.MinScore.Value);
        if (q.MaxScore.HasValue) queryable = queryable.Where(l => l.Score <= q.MaxScore.Value);
        if (q.Purchased.HasValue) queryable = queryable.Where(l => l.Purchased == q.Purchased.Value);

        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            var s = q.Search.Trim();
            queryable = queryable.Where(l =>
                (l.User.Name != null && EF.Functions.ILike(l.User.Name, $"%{s}%")) ||
                (l.User.Email != null && EF.Functions.ILike(l.User.Email, $"%{s}%")) ||
                (l.User.Phone != null && EF.Functions.ILike(l.User.Phone, $"%{s}%")));
        }

        return await queryable
            .OrderByDescending(l => l.Score)
            .Select(l => new GetLeadDto
            {
                Id = l.Id,
                UserId = l.UserId,
                UserName = l.User.Name,
                UserEmail = l.User.Email,
                UserPhone = l.User.Phone,
                IsRegistered = l.IsRegistered,
                SubscriptionOpenedCount = l.SubscriptionOpenedCount,
                Purchased = l.Purchased,
                Priority = l.Priority,
                OperatorId = l.OperatorId,
                OperatorName = l.Operator != null ? l.Operator.Name : null,
                Status = l.Status,
                Score = l.Score,
                Temperature = l.Temperature,
                LastActivity = l.LastActivity,
                LastContactedAt = l.LastContactedAt,
                NextFollowUpAt = l.NextFollowUpAt,
                FollowUpOverdue = l.NextFollowUpAt != null && l.NextFollowUpAt < now,
                CreatedAt = l.CreatedAt
            })
            .GetByDataQueryAsync(q);
    }

    public async Task<GetLeadDetailDto> GetByIdAsync(long id, long? operatorScopeId)
    {
        var lead = await context.Leads
                       .Where(l => l.Id == id)
                       .Select(l => new GetLeadDetailDto
                       {
                           Id = l.Id,
                           UserId = l.UserId,
                           UserName = l.User.Name,
                           UserEmail = l.User.Email,
                           UserPhone = l.User.Phone,
                           IsRegistered = l.IsRegistered,
                           SubscriptionOpenedCount = l.SubscriptionOpenedCount,
                           Purchased = l.Purchased,
                           Priority = l.Priority,
                           OperatorId = l.OperatorId,
                           OperatorName = l.Operator != null ? l.Operator.Name : null,
                           Status = l.Status,
                           Score = l.Score,
                           Temperature = l.Temperature,
                           LastActivity = l.LastActivity,
                           LastContactedAt = l.LastContactedAt,
                           NextFollowUpAt = l.NextFollowUpAt,
                           CreatedAt = l.CreatedAt,
                           PaymentProvider = l.PaymentProvider,
                           WonAmount = l.WonAmount,
                           WonAt = l.WonAt,
                           Age = l.User.Extra != null ? l.User.Extra.Age : (int?)null,
                           Gender = l.User.Extra != null ? l.User.Extra.Gender : (EnumGender?)null,
                           Weight = l.User.Extra != null ? l.User.Extra.Weight : (double?)null,
                           Height = l.User.Extra != null ? l.User.Extra.Height : (double?)null,
                           Purpose = l.User.Extra != null ? l.User.Extra.Purpose : (EnumPurpose?)null
                       })
                       .FirstOrDefaultAsync()
                   ?? throw new LeadNotFoundException();

        if (operatorScopeId.HasValue && lead.OperatorId.HasValue && lead.OperatorId != operatorScopeId)
            throw new ForbiddenException();

        return lead;
    }

    public async Task<IEnumerable<GetLeadActivityDto>> GetTimelineAsync(long leadId)
    {
        if (!await context.Leads.AnyAsync(l => l.Id == leadId))
            throw new LeadNotFoundException();

        return await context.LeadActivities
            .Where(a => a.LeadId == leadId)
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => new GetLeadActivityDto
            {
                Id = a.Id,
                Type = a.Type,
                Description = a.Description,
                ActorName = a.Actor != null ? a.Actor.Name : null,
                CreatedAt = a.CreatedAt
            })
            .ToListAsync();
    }

    #endregion

    #region Escalation job

    /// <summary>
    /// Recurring safety net: surfaces overdue follow-ups so no hot lead is forgotten.
    /// </summary>
    [AutomaticRetry(Attempts = 1)]
    public async Task EscalateLeadsAsync()
    {
        var now = DateTime.Now;

        var overdue = await context.FollowUps
            .Where(f => !f.IsDone && !f.Escalated && f.DueAt < now)
            .ToListAsync();

        foreach (var f in overdue)
        {
            f.Escalated = true;
            context.Add(new LeadActivity
            {
                LeadId = f.LeadId,
                Type = EnumLeadActivityType.FollowUpDue,
                Description = "Follow-up vaqti keldi"
            });
        }

        if (overdue.Count > 0)
            await context.SaveChangesAsync();

        logger.LogInformation("EscalateLeads processed {Count} overdue follow-ups", overdue.Count);
    }

    #endregion
}
