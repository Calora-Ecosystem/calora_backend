namespace Core.Services.Billing.Payme.Contracts;

public class CancelTransactionRequestDto
{
    public string Id { get; set; } = null!;
    public short Reason { get; set; }
}