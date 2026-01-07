using System.ComponentModel.DataAnnotations.Schema;
using BRB.Core.Common.Models.Base;
using Core.Entities.Auth;
using Core.Entities.Billing.Enum;
using Microsoft.EntityFrameworkCore;

namespace Core.Entities.Billing;

[Index(nameof(Status))]
[Index(nameof(Type))]
public class Order : AuditableModelBase<long>
{
    [ForeignKey(nameof(User))] public long UserId { get; set; }
    public required long Amount { get; set; }
    public EnumPaymentProviders Provider { get; set; }
    public EnumOrderType Type { get; set; }
    public EnumOrderStatus Status { get; set; }
    public long TransactionId { get; set; }
    public User User { get; set; } = null!;
}