namespace Core.Services.User.Contracts;

public record GetAllUsersDto
{
    public long Id { get; set; }
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public List<string> Roles { get; set; } = null!;
    public SubscriptionDto? Subscription { get; set; }
    public UserExtraShortDto? Extra { get; set; }
}