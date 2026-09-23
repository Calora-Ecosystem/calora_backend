using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BRB.Core.Common.Models.Base;
using Core.Entities.Auth;
using Core.Entities.Coins.Enum;
using Microsoft.EntityFrameworkCore;

namespace Core.Entities.Coins;

/// <summary>
/// Hamyon tarixi. <see cref="Amount"/> ishorali: musbat — kirim, manfiy — chiqim.
/// <see cref="Title"/> mobile lokalizatsiya kaliti (masalan <c>coin_tx_daily_steps</c>, <c>mi_premium_30</c>).
/// Qadam coinlari kuniga bitta yozuv (<see cref="EnumCoinTxType.Steps"/>, <see cref="RefId"/> = yyyyMMdd),
/// kun davomida qadam oshgani sari shu yozuv oshiriladi.
/// </summary>
[Index(nameof(UserId), nameof(CreatedAt))]
[Index(nameof(Type), nameof(CreatedAt))]
[Index(nameof(UserId), nameof(Type), nameof(RefId), IsUnique = true)]
public class CoinTransaction : AuditableModelBase<long>
{
    [ForeignKey(nameof(User))] public long UserId { get; set; }
    public long Amount { get; set; }
    public EnumCoinTxType Type { get; set; }
    [MaxLength(100)] public string Title { get; set; } = null!;

    /// <summary>
    /// Bog'liq obyekt: qadam kuni (yyyyMMdd), market purchase id, referral id va h.k.
    /// (UserId, Type, RefId) unique — bir kun/xarid ikki marta yozilmaydi.
    /// </summary>
    public long? RefId { get; set; }

    public User User { get; set; } = null!;
}
