using BRB.Core.Common.Models.Base;
using Core.Enums;
using Microsoft.EntityFrameworkCore;

namespace Core.Entities.Billing;

public class SubscriptionOrder : ModelBase<long>
{
    public long OrderId { get; set; }
    public EnumSPlans Plan { get; set; }
    public long PlanExtraId { get; set; }
    public Order Order { get; set; } = null!;
    [DeleteBehavior(DeleteBehavior.Restrict)]
    public PlanExtra PlanExtra { get; set; } = null!;
}