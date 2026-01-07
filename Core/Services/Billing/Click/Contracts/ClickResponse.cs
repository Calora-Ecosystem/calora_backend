using System.Text.Json.Serialization;

namespace Core.Services.Billing.Click.Contracts;

public class ClickResponse
{
    [JsonPropertyName("click_trans_id")]
    public long? ClickTransId { get; set; }
    [JsonPropertyName("merchant_trans_id")]
    public string? OrderId { get; set; }
    [JsonPropertyName("merchant_prepare_id")]
    public uint? MerchantPrepareId { get; set; }
    [JsonPropertyName("merchant_confirm_id")]
    public uint? MerchantConfirmId { get; set; }
    [JsonPropertyName("error")]
    public ClickErrorType? Error { get; set; }
    [JsonPropertyName("error_note")] 
    public string? ErrorNote { get; set; }
}