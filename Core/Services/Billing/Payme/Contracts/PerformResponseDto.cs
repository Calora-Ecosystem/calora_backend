using System.Text.Json.Serialization;

namespace Core.Services.Billing.Payme.Contracts;

public class PerformResponseDto
{
    public string Transaction { get; set; } = null!;
    [JsonPropertyName("perform_time")] public long PerformTime { get; set; }
    public int State { get; set; }
}