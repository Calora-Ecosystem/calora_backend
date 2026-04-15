using System.Text.Json.Serialization;

namespace Core.Services.Billing.Payme.Contracts;

public class CheckTransactionResponseDto
{
    [JsonPropertyName("create_time")] public long CreateTime { get; set; }
    [JsonPropertyName("perform_time")] public long PerformTime { get; set; }
    [JsonPropertyName("cancel_time")] public long CancelTime { get; set; }
    public string Transaction { get; set; } = null!;
    public int State { get; set; }
    public int? Reason { get; set; } = null!;
}