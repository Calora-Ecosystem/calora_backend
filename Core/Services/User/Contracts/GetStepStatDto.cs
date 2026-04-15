namespace Core.Services.User.Contracts;

public record GetStepStatDto
{
    public UserDto User { get; init; }
    public double Sum { get; init; }
    public int Count { get; init; }
    public int Index { get; init; }
}