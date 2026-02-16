using System.ComponentModel.DataAnnotations;

namespace Core.Services.Billing.Rc.Contracts;

public record RcConfig
{
    [Required] public string Login { get; set; } = null!;
    [Required] public string Password { get; set; } = null!;
}