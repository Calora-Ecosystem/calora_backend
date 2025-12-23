using System.ComponentModel.DataAnnotations.Schema;
using BRB.Core.Common.Models.Base;
using Core.Entities.Billing.Enum;

namespace Core.Entities.Billing;

public class ClickTransaction : AuditableModelBase<uint>
{
    public required EnumClickTransactionState State { get; set; }
    public string Currency { get; set; } = "UZB";
    public required decimal Total { get; set; }
    public required decimal Amount { get; set; }
    public uint Delivery { get; set; }
    public uint Tax { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? UserId { get; set; }
    public string? InvoiceId { get; set; }
    public string? PaymentId { get; set; }
    public string? CardToken { get; set; }
    public required string Token { get; set; }
    public string? PhoneNumber { get; set; }
    public string Note { get; set; } = string.Empty;

    [ForeignKey(nameof(Order))] public required long OrderId { get; set; }

    public Order Order { get; set; } = default!;
}