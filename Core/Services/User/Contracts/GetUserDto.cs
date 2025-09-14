namespace Core.Services.User;

public record GetUserDto(long Id, string Email, List<string> Roles);