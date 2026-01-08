namespace Core.Services.Billing.Payme.Contracts;

public class GetStatementResponseDto
{
    public List<GetStatementTransaction> Transactions { get; set; } = null!;
}

public class GetStatementTransaction
{
    public string Id { get; set; } = null!;
    public long Time { get; set; }
    public long Amount { get; set; }
    public AccountBaseDto AccountBaseDto { get; set; } = null!;
    public long CreateTime { get; set; }
    public long PerformTime { get; set; }
    public long CancelTime { get; set; }
    public string Transaction { get; set; } = null!;
    public int State { get; set; }
    public int? Reason { get; set; }
}