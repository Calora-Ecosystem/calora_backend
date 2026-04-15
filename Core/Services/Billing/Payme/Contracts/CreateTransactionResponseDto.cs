using System.Text.Json.Serialization;
using Core.Entities.Billing.Payme;

namespace Core.Services.Billing.Payme.Contracts;

public class CreateTransactionResponseDto
{
    [JsonPropertyName("create_time")] public long CreateTime { get; set; }

    public string Transaction { get; set; } = null!;
    public int State { get; set; }
}