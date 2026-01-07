using System.ComponentModel.DataAnnotations;

namespace Core.Services.Billing.Payme.Contracts;

public class PaymeConfig
{
    public string Login { get; set; } = null!;
    [Required] public string AuthToken { get; set; } = null!;
}