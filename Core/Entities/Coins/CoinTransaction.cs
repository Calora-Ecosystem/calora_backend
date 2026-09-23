using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BRB.Core.Common.Models.Base;
using Core.Entities.Auth;
using Core.Entities.Coins.Enum;
using Microsoft.EntityFrameworkCore;

namespace Core.Entities.Coins;

/// <summary>
/// Hamyon tarixi. <see cref="Amount"/> ishorali: musbat — kirim, manfiy — chiqim.
/// <see cref="Title"/> mobile lokalizatsiya kaliti (masalan <c>coin_tx_from_calora</c>, <c>mi_premium_7</c>).
/// </summary>
[Index(nameof(UserId), nameof(CreatedAt))]
[Index(nameof(Type), nameof(CreatedAt))]
public class CoinTransaction : AuditableModelBase<long>
{
    [ForeignKey(nameof(User))] public long UserId { get; set; }
    public long Amount { get; set; }
    public EnumCoinTxType Type { get; set; }
    [MaxLength(100)] public string Title { get; set; } = null!;

    /// <summary>Bog'liq obyekt id-si (market purchase, referral va h.k.).</summary>
    public long? RefId { get; set; }

    /// <summary>Almashtirishda sarflangan Calora miqdori.</summary>
    public long? Calora { get; set; }

    public User User { get; set; } = null!;
}
