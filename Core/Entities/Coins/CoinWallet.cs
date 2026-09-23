using System.ComponentModel.DataAnnotations.Schema;
using BRB.Core.Common.Models.Base;
using Core.Entities.Auth;
using Microsoft.EntityFrameworkCore;

namespace Core.Entities.Coins;

/// <summary>
/// Calora coin hamyoni (1 user = 1 hamyon). Balans faqat atomik
/// <c>ExecuteUpdate</c> orqali o'zgartiriladi (<see cref="Core.Services.Coins.CoinService"/>),
/// har bir o'zgarish <see cref="CoinTransaction"/> jadvaliga yoziladi.
/// </summary>
[Index(nameof(UserId), IsUnique = true)]
public class CoinWallet : AuditableModelBase<long>
{
    [ForeignKey(nameof(User))] public long UserId { get; set; }

    /// <summary>Joriy coin balansi.</summary>
    public long Balance { get; set; }

    /// <summary>Umr bo'yi ishlab topilgan coinlar.</summary>
    public long TotalEarned { get; set; }

    /// <summary>Umr bo'yi sarflangan coinlar.</summary>
    public long TotalSpent { get; set; }

    /// <summary>Coinga almashtirilgan Calora (qadamdan yoqilgan kkal) miqdori.</summary>
    public long CaloraExchanged { get; set; }

    public User User { get; set; } = null!;
}
