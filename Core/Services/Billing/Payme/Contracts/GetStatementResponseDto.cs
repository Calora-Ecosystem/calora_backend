using System.Text.Json.Serialization;

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
    [JsonPropertyName("create_time")] public long CreateTime { get; set; }
    [JsonPropertyName("perform_time")] public long PerformTime { get; set; }
    [JsonPropertyName("cancel_time")] public long CancelTime { get; set; }
    public string Transaction { get; set; } = null!;
    public int State { get; set; }
    public int? Reason { get; set; }
}