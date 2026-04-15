using System.ComponentModel.DataAnnotations;

namespace Core.Services.Billing.Payme.Contracts;

public class PaymeConfig
{
    [Required] public string CheckoutUrl { get; set; } = null!;
    [Required] public string MerchantId { get; set; } = null!;
    public string Login { get; set; } = null!;
    [Required] public string AuthToken { get; set; } = null!;
}