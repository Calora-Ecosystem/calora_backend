namespace Core.Services.User.Contracts;

public record GetStepStatDto(UserDto User, double Sum, int Count, int Index);