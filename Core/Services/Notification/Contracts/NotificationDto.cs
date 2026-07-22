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

public class EmailNotificationWithoutUserDto : NotificationDto
{
    public string Email { get; set; } = null!;
}

public class BatchEmailNotificationDto : NotificationDto
{
    public IReadOnlyCollection<long> UserIds { get; set; } = null!;
}

public class PushNotificationDto : NotificationDto
{
    public long? Id { get; set; }
    public long UserId { get; set; }
    public string? Image { get; set; }
    public DateTime? Scheduled { get; set; }
    public Dictionary<string, string>? Meta { get; set; }

    /// <summary>When set, the push is skipped at send time if the user already logged this menu today.</summary>
    public Core.Enums.EnumMenu? MealGateMenu { get; set; }
}

public class SmsNotificationDto : NotificationDto
{
    public string Phone { get; set; } = null!;
}