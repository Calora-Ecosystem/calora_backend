using System.ComponentModel.DataAnnotations.Schema;
using BRB.Core.Common.Models.Base;
using Core.Entities.Auth;
using Core.Entities.Refs;
using Core.Enums;

namespace Core.Entities.Notification;

public class Reminder : ModelBase<long>
{
    [ForeignKey(nameof(User))] public long UserId { get; set; }
    [ForeignKey(nameof(Moment))] public long MomentId { get; set; }
    public TimeSpan Before { get; set; }

    public Moment Moment { get; set; } = default!;
    public User User { get; set; } = default!;
}