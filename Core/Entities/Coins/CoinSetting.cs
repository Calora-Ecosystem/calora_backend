using System.ComponentModel.DataAnnotations.Schema;
using BRB.Core.Common.Models.Base;
using Core.Entities.Auth;
using Microsoft.EntityFrameworkCore;

namespace Core.Entities.Coins;

/// <summary>
/// Coin tizimining dashboard'dan boshqariladigan sozlamasi (bitta qator). <see cref="EarnStartDate"/> —
/// qaysi kundan boshlab qadam coin beradi; undan oldingi kunlar hisoblanmaydi, kelajak sana qo'yilsa
/// o'sha kungacha hech kimga coin yozilmaydi. Qator bo'lmasa <c>CoinConfig.CoinsEarnStartDate</c>.
/// </summary>
public class CoinSetting : AuditableModelBase<long>
{
    /// <summary>Coin hisoblash boshlanadigan kun (vaqtsiz, server kuni).</summary>
    public DateTime EarnStartDate { get; set; }

    [ForeignKey(nameof(UpdatedBy))] public long? UpdatedById { get; set; }

    [DeleteBehavior(DeleteBehavior.SetNull)] public User? UpdatedBy { get; set; }
}
