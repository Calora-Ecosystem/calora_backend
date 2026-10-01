using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BRB.Core.Common.Models.Base;
using Core.Entities.Auth;
using Microsoft.EntityFrameworkCore;

namespace Core.Entities.Coins;

/// <summary>
/// Qadam → coin qoidasi (dashboard'dan SuperAdmin boshqaradi): har <see cref="StepsPerCoin"/> qadam = 1 coin,
/// kuniga ko'pi bilan <see cref="MaxDailyCoins"/>. Qoida <see cref="EffectiveFrom"/> kunidan boshlab amal qiladi —
/// o'tgan kunlar o'z davridagi qoida bo'yicha hisoblanadi, shuning uchun yangi qoida eski kunlarga
/// orqaga qarab coin bermaydi. Hech qanday qoida bo'lmasa <c>CoinConfig</c> qiymatlari ishlatiladi.
/// </summary>
[Index(nameof(EffectiveFrom), IsUnique = true)]
public class CoinRule : AuditableModelBase<long>
{
    public int StepsPerCoin { get; set; }
    public int MaxDailyCoins { get; set; }

    /// <summary>Qoida amal qila boshlaydigan kun (vaqtsiz, server kuni).</summary>
    public DateTime EffectiveFrom { get; set; }

    [MaxLength(300)] public string? Note { get; set; }

    [ForeignKey(nameof(CreatedBy))] public long? CreatedById { get; set; }

    [DeleteBehavior(DeleteBehavior.SetNull)] public User? CreatedBy { get; set; }
}
