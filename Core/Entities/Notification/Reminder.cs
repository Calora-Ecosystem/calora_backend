using System.ComponentModel.DataAnnotations.Schema;
using BRB.Core.Common.Models.Base;
using Core.Entities.Auth;
using Core.Enums;

namespace Core.Entities.Notification;

public class Reminder : ModelBase<long>
{
    [ForeignKey(nameof(User))] public long UserId { get; set; }
    // [ForeignKey(nameof(Moment))] public long MomentId { get; set; }
    public TimeSpan Time { get; set; }

    public EnumMomentType Type { get; set; }
    public EnumMenu? Menu { get; set; }

    // public Moment Moment { get; set; } = default!;
    public User User { get; set; } = default!;
}