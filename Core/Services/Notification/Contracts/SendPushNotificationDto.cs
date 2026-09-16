namespace Core.Services.Notification.Contracts;

public class SendPushNotificationDto : PushNotificationDto
{
    /// <summary>
    /// Agar true bo'lsa, tizimdagi barcha faol foydalanuvchilarga yuboriladi.
    /// </summary>
    public bool AllUsers { get; set; } = false;

    /// <summary>
    /// Agar AllUsers false bo'lsa, ushbu user ID ro'yxatiga yuboriladi.
    /// </summary>
    public List<long>? UserIds { get; set; }
}
