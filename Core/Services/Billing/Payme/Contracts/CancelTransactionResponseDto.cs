using System.Text.Json.Serialization;

namespace Core.Services.Billing.Payme.Contracts;

public class CancelTransactionResponseDto
{
    public string Transaction { get; set; } = null!;
    [JsonPropertyName("cancel_time")] public long CancelTime { get; set; }
    public int State { get; set; }
}