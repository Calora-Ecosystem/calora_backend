using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BRB.Core.Common.Models.Base;
using Microsoft.EntityFrameworkCore;

namespace Core.Entities.Billing.Payme;

[Index(nameof(ExternalId), IsUnique = true)]
public class PaymeTransaction : AuditableModelBase<uint>
{
    [MaxLength(25)] public string? ExternalId { get; set; } = null!;
    public DateTime? ExternalCreatedAt { get; set; }
    public long Amount { get; set; }
    [ForeignKey(nameof(Order))] public required long OrderId { get; set; }

    public EnumPaymeTransactionStatus Status { get; set; }

    public DateTime? PerformedAt { get; set; }
    public DateTime? CancelledAt { get; set; }

    public string? Reason { get; set; }

    [DeleteBehavior(DeleteBehavior.Restrict)]
    public Order Order { get; set; } = default!;
}