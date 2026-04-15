using Core.Enums;

namespace Core.Services.Notification.Contracts;

public class AddRemindDto
{
    public TimeOnly Time { get; set; }
    public EnumMomentType Type { get; set; }
    public EnumMenu? Menu { get; set; }
}