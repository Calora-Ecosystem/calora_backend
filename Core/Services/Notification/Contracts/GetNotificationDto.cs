namespace Core.Services.Notification.Contracts;

public record GetNotificationDto
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public bool HasRead { get; set; }
    public DateTime SentAt { get; set; }
    public string? Image { get; set; }
}