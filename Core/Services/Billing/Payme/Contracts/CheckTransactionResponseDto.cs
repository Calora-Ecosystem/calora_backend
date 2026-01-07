namespace Core.Services.Billing.Payme.Contracts;

public class CheckTransactionResponseDto
{
    public long CreateTime { get; set; }
    public long PerformTime { get; set; }
    public long CancelTime { get; set; }
    public string Transaction { get; set; } = null!;
    public int State { get; set; }
    public string? Reason { get; set; } = null!;
}