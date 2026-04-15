using Core.Enums;

namespace Core.Services.User.Contracts;

public record GetUserExtraDto(
    long UserId,
    double Weight,
    double EntryWeight,
    double Height,
    double Bmi,
    EnumGender Gender,
    DateTime BirthDate,
    string? Photo,
    string Name,
    EnumActivityLevel ActivityLevel,
    EnumPurpose Purpose,
    EnumPhysicalActivity? PhysicalActivity)
{
    public List<UserProgressSummaryDto> Progress { get; set; } = null!;
}