using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BRB.Core.Common.Models.Base;
using Core.Entities.Auth;

namespace Core.Entities.Crm;

public class Note : AuditableModelBase<long>
{
    public long OperatorId { get; set; }
    public long LeadId { get; set; }

    [MaxLength(500)]
    public string Text { get; set; } = null!;

    [ForeignKey(nameof(LeadId))] public Lead Lead { get; set; } = null!;

    [ForeignKey(nameof(OperatorId))] public User Operator { get; set; } = null!;
}