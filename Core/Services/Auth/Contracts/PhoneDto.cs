using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using BRB.Core.Common.Attributes;

namespace Core.Services.Auth.Contracts;

public class PhoneDto
{
    [LocalPhone(ErrorMessage = "E-mail must be valid")]
    [MaxLength(100)]
    [DefaultValue("+998998887766")]
    public string Phone { get; set; } = null!;
}