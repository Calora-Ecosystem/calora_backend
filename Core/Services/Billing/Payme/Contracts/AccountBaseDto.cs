using System.Text.Json.Serialization;

namespace Core.Services.Billing.Payme.Contracts;

public class AccountBaseDto
{
    [JsonPropertyName("order_id")] public string OrderId { get; set; } = null!;
}