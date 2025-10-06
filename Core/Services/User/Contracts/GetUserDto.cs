namespace Core.Services.User.Contracts;

public record GetUserDto(long Id, string Email, List<string> Roles);