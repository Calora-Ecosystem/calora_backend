using System.ComponentModel.DataAnnotations;

namespace Core.Services.Auth.Contracts;

public class SignInDto : EmailDto
{
    public Guid VerificationCode { get; set; }

    [Length(6, 6, ErrorMessage = "Verification code length should be 6")]
    public string Code { get; set; } = default!;

    [Required] public DeviceDto DeviceInfo { get; set; } = null!;
}