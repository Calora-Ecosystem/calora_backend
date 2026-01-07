namespace Core.Services.Billing.Payme.Contracts;

public class CheckPerformTransactionDto
{
    public long Amount { get; set; }
    public AccountBaseDto Account { get; set; } = null!;
}