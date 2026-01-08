namespace Core.Services.Billing.Payme.Contracts;

public class GetStatementRequestDto
{
    public long From { get; set; }
    public long To { get; set; }
}