namespace Core.Services.Billing.Contracts;

public record CreateSubscriptionOrderResponseDto
{
    public bool PaymentRequired { get; set; }
    public string PaymentLink { get; set; } = null!;
}