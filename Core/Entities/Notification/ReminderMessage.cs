using System.ComponentModel.DataAnnotations;
using BRB.Core.Common.Models.Base;
using Core.Enums;

namespace Core.Entities.Notification;

public class ReminderMessage : ModelBase<long>
{
    public EnumMomentType Type { get; set; }
    public EnumMenu? Menu { get; set; }
    [MaxLength(100)]
    public string Title { get; set; } = null!;
    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// Global reminder time. When set together with <see cref="IsActive"/>, the reminder is
    /// dispatched to all users at this time of day. Null means the message is only used as a
    /// text template for per-user reminders.
    /// </summary>
    public TimeSpan? Time { get; set; }

    /// <summary>
    /// Whether the global (dashboard-scheduled) reminder is enabled.
    /// </summary>
    public bool IsActive { get; set; }
}