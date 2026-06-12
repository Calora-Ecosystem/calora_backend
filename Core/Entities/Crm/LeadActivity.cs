using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BRB.Core.Common.Models.Base;
using Core.Entities.Auth;
using Core.Entities.Crm.Enum;
using Microsoft.EntityFrameworkCore;

namespace Core.Entities.Crm;

[Index(nameof(LeadId))]
public class LeadActivity : ModelBase<long>
{
    public long LeadId { get; set; }

    // Operator who performed the action; null for system/app-generated events.
    public long? ActorId { get; set; }

    public EnumLeadActivityType Type { get; set; }

    [MaxLength(500)] public string Description { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [ForeignKey(nameof(LeadId))] public Lead Lead { get; set; } = null!;
    [ForeignKey(nameof(ActorId))] public User? Actor { get; set; }
}
