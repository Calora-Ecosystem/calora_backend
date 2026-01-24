using Swashbuckle.AspNetCore.Annotations;

namespace Core.Services.User.Contracts;

public record GetUserDto(long Id, [SwaggerSchema("Email or Phone")]string? Email, List<string> Roles);