using System.ComponentModel.DataAnnotations;
using Core.Enums;

namespace Core.Services.User.Contracts;

public class CreateUserExtraDto
{
    public string Name { get; set; } = null!;
    [Range(20, int.MaxValue)]
    public double Weight { get; set; }
    [Range(20, int.MaxValue)]
    public double Height { get; set; }
    public EnumGender Gender { get; set; }
    public DateTime BirthDate { get; set; }
    public string? Photo { get; set; }
    public EnumLanguage Language { get; set; }
    public EnumActivityLevel ActivityLevel { get; set; }
    public EnumPurpose Purpose { get; set; }
}