using System.ComponentModel.DataAnnotations;

namespace Core.Services.Auth.Contracts;

public class SsoSignInDto
{
    [Required] public string SsoToken { get; set; } = null!;
    [Required] public DeviceDto DeviceInfo { get; set; } = null!;
}