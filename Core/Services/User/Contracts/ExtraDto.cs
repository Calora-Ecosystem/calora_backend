using Core.Enums;

namespace Core.Services.User.Contracts;

public record ExtraDto(string? Photo, EnumActivityLevel ActivityLevel);