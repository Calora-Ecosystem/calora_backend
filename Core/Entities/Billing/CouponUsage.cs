using BRB.Core.Common.Models.Base;
using Microsoft.EntityFrameworkCore;

namespace Core.Entities.Billing;

[Index(nameof(CouponId), nameof(UserId), nameof(OrderId))]
public class CouponUsage : AuditableModelBase<long>
{
    public long CouponId { get; set; }
    public long UserId { get; set; }
    public long OrderId { get; set; }
    public long Amount { get; set; }
}