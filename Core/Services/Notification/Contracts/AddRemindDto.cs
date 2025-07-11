namespace Core.Services.Notification.Contracts;

public class AddRemindDto
{
    public long MomentId { get; set; }
    public int BeforeInMinutes { get; set; }
}