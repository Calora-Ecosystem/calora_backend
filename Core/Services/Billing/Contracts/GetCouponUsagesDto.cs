namespace Core.Services.Billing.Contracts;

public record GetCouponUsagesDto
{
    public long OrderId { get; set; }
    public string UserName { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public long Amount { get; set; }
}