namespace Core.Services.Billing.Payme.Contracts;

public class CreateTransactionDto
{
    public AccountBaseDto Account { get; set; } = null!;
    public long Amount { get; set; }
    public string Id { get; set; } = null!;
    public long Time { get; set; }
}