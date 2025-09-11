using System.ComponentModel.DataAnnotations.Schema;

namespace Core.Entities.Auth;

public class UserStepStat
{
    [ForeignKey(nameof(UserId))] public long UserId { get; set; }
    public double Sum { get; set; }
    public int Count { get; set; }
    public int Index { get; set; }

    public User User { get; set; } = null!;
}