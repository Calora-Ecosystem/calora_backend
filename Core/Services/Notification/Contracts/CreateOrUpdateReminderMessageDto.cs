using System.ComponentModel.DataAnnotations;
using Core.Enums;

namespace Core.Services.Notification.Contracts;

public class CreateOrUpdateReminderMessageDto
{
    public long? Id { get; set; }
    public EnumMomentType Type { get; set; }
    public EnumMenu? Menu { get; set; }
    [MaxLength(100)]
    public string Title { get; set; } = null!;
    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>Global reminder time of day. Null = no schedule (text template only).</summary>
    public TimeOnly? Time { get; set; }

    /// <summary>Whether the global reminder is enabled.</summary>
    public bool IsActive { get; set; }
}
