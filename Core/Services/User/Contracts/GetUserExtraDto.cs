using Core.Enums;

namespace Core.Services.User.Contracts;

public record GetUserExtraDto(
    long UserId,
    double Weight,
    double Height,
    double Bmi,
    EnumGender Gender,
    DateTime BirthDate,
    string? Photo,
    string Name,
    EnumActivityLevel ActivityLevel)
{
    public List<UserProgressSummaryDto> Progress { get; set; } = null!;
}