namespace Core.Services.User.Contracts;

public record UserDto(long Id, string Name, string Email, ExtraDto? Extra);