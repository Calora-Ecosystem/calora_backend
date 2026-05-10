using BRB.Core.Common.Models;
using BRB.Core.EF.Attributes;
using BRB.Core.EF.Extensions;
using Core.Brokers.DbContext;
using Core.Entities.Crm;
using Core.Entities.Crm.Enum;
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
    
    [AutomaticRetry(Attempts = 3)]
    public async Task HandleEventAsync(HandleLeadEventDto dto)
    {
        var lead = await context.Leads.FirstOrDefaultAsync(l => l.UserId == dto.UserId && l.Priority != EnumLeadPriority.Closed) ?? context.Add(new Lead()
        {
            UserId = dto.UserId,
        }).Entity;

        switch (dto.Event)
        {
            case EnumLeadEvent.Registered:
                lead.IsRegistered = true;
                break;

            case EnumLeadEvent.SubscriptionOpened:
                if (!lead.IsRegistered)
                    lead.IsRegistered = true;
                
                lead.SubscriptionOpenedCount++;
                break;

            case EnumLeadEvent.Purchased:
                lead.Purchased = true;
                break;

            default:
                logger.LogWarning("Unknown LeadEvent {Event} for UserId {UserId}", dto.Event, dto.UserId);
                return;
        }

        lead.LastActivity = DateTime.Now;
        lead.Priority = GetLeadPriority(lead);
        await context.SaveChangesAsync();

        logger.LogInformation("LeadEvent {Event} applied for UserId {UserId}", dto.Event, dto.UserId);
    }
    
    public EnumLeadPriority GetLeadPriority(Lead lead)
    {
        //Agar registratsiya qilmagan bo'lsa
        if (lead is { IsRegistered: false })
            return EnumLeadPriority.Low;

        //Registratsiya qilgan ammo hali buyurtmani ochmagan
        if (lead is { SubscriptionOpenedCount: <= 0 })
            return EnumLeadPriority.Low;

        //Sotib olib bo'lgan
        if (lead is { Purchased: true })
            return EnumLeadPriority.Closed;

        //Buyurtmani ochgan ammo hali olgani yo'q
        return EnumLeadPriority.High;
    }
    
    public async Task<long> CreateOrUpdateNote(long leadId, long operatorId, UpsertNoteDto dto)
    {
        if(!await context.Leads.AnyAsync(l => l.Id == leadId))
            throw new LeadNotFoundException();
        
        var note = dto.Id.HasValue
            ? await context.Notes.FirstOrDefaultAsync(n => n.Id == dto.Id.Value)
              ?? throw new NoteNotFoundException()
            : context.Add(new Note { LeadId = leadId, OperatorId = operatorId }).Entity;

        if (dto.Id.HasValue && note.OperatorId != operatorId)
            throw new NoteAccessDeniedException();

        note.Text = dto.Text;
        
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

    public async Task<Wrapper> GetAllAsync(GetLeadsQuery q)
    {
        var queryable = context.Leads.AsQueryable();

        if (q.Priority.HasValue)
            queryable = queryable.Where(l => l.Priority == q.Priority.Value);

        return await queryable
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
                LastActivity = l.LastActivity
            })
            .GetByDataQueryAsync(q);
    }

    public async Task<GetLeadDetailDto> GetByIdAsync(long id)
    {
        return await context.Leads
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
                       LastActivity = l.LastActivity
                   })
                   .FirstOrDefaultAsync()
               ?? throw new LeadNotFoundException();
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
}