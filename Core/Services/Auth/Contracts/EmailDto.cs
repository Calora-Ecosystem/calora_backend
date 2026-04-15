using System.ComponentModel.DataAnnotations;

namespace Core.Services.Auth.Contracts;

public class EmailDto
{
    [EmailAddress(ErrorMessage = "E-mail must be valid")]
    [MaxLength(100)]
    public string Email { get; set; } = null!;
}