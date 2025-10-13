namespace Core.Services.User.Contracts;

public record GetStepMetricsDto(long UserId, UserDto User, double Foots, double Distance, double Kcal);