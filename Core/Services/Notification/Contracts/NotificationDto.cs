namespace Core.Services.Notification.Contracts;

public class NotificationDto
{
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
}

public class NotificationWithUserDto : NotificationDto
{
    public long UserId { get; set; }
}

public class EmailNotificationDto : NotificationWithUserDto
{
    public string Email { get; set; } = null!;
}

public class BatchEmailNotificationDto : NotificationDto
{
    public IReadOnlyCollection<long> UserIds { get; set; } = null!;
}

public class PushNotificationDto : NotificationWithUserDto
{
    public IReadOnlyCollection<string> Images { get; set; } = null!;
}