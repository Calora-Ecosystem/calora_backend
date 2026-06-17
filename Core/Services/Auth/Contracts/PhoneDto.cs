using System.ComponentModel;
using Core.Attributes;

namespace Core.Services.Auth.Contracts;

public class PhoneDto
{
    [LocalPhone]
    [DefaultValue("+998998887766")]
    public string Phone { get; set; } = null!;
}