using BRB.Core.Common.Models.Base;
using Core.Enums;

namespace Core.Entities.Billing;

public class SubscriptionOrder : ModelBase<long>
{
    public long OrderId { get; set; }
    public EnumSPlans Plan { get; set; }
    public Order Order { get; set; } = null!;
}