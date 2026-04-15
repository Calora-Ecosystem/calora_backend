using System.ComponentModel.DataAnnotations;

namespace Core.Services.Billing.Click;

public class ClickConfig
{
    [Required] public required string MerchantId { get; set; }
    [Required] public required string ServiceId { get; set; }
    [Required] public required string UserId { get; set; }
    [Required] public required string SecretKey { get; set; }
}