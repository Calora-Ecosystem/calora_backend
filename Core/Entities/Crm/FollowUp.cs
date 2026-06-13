using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BRB.Core.Common.Models.Base;
using Core.Entities.Auth;
using Microsoft.EntityFrameworkCore;

namespace Core.Entities.Crm;

[Index(nameof(DueAt))]
[Index(nameof(IsDone))]
public class FollowUp : ModelBase<long>
{
    public long LeadId { get; set; }
    public long OperatorId { get; set; }

    public DateTime DueAt { get; set; }

    [MaxLength(500)] public string? Note { get; set; }

    public bool IsDone { get; set; }
    public DateTime? DoneAt { get; set; }

    // Set once the escalation job has flagged this follow-up as due/overdue.
    public bool Escalated { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [ForeignKey(nameof(LeadId))] public Lead Lead { get; set; } = null!;
    [ForeignKey(nameof(OperatorId))] public User Operator { get; set; } = null!;
}
