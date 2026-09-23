using System.ComponentModel.DataAnnotations.Schema;
using Core.Entities.Auth;

namespace Core.Entities.Coins;

/// <summary>Coin reytingi uchun keyless natija (<see cref="UserStepStat"/> kabi).</summary>
[NotMapped]
public class UserCoinStat
{
    [ForeignKey(nameof(UserId))] public long UserId { get; set; }
    public long Sum { get; set; }
    public int Index { get; set; }

    public User User { get; set; } = null!;
}
