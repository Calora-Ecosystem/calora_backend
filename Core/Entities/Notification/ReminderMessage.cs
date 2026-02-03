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
}