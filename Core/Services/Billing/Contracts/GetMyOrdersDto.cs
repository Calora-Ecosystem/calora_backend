using Core.Entities.Billing.Enum;

namespace Core.Services.Billing.Contracts;

public record GetMyOrdersDto
{
    public long Id { get; set; }
    public EnumOrderStatus Status { get; set; }
    public long Amount { get; set; }
    public EnumOrderType Type { get; set; }
    public EnumPaymentProviders Provider { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}