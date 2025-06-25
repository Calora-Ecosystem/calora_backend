using System.ComponentModel.DataAnnotations.Schema;
using Core.Enums;

namespace Core.Entities.Auth;

public class UserNorm
{
    [ForeignKey(nameof(User))] public long UserId { get; set; }
    public EnumMetrics Metric { get; set; }
    public double Value { get; set; }

    public User User { get; set; } = null!;
}