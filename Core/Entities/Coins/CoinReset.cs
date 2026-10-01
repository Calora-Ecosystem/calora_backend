using System.ComponentModel.DataAnnotations.Schema;
using BRB.Core.Common.Models.Base;
using Core.Entities.Auth;
using Microsoft.EntityFrameworkCore;

namespace Core.Entities.Coins;

/// <summary>
/// Dashboard'dan "barcha userlarning coinlarini o'chirish" jurnali: hamma hamyon 0 ga tushirilgan va
/// coin tarixi o'chirilgan vaqt, o'sha paytdagi hisoblash kuni va qancha coin o'chgani.
/// Do'kon xaridlari (<see cref="MarketPurchase"/>) va berilgan Premium o'chirilmaydi.
/// </summary>
public class CoinReset : AuditableModelBase<long>
{
    /// <summary>Reset bilan birga o'rnatilgan coin hisoblash kuni.</summary>
    public DateTime EarnStartDate { get; set; }

    /// <summary>Coini bo'lgan (balans yoki tarixi bor) userlar.</summary>
    public int UsersAffected { get; set; }

    /// <summary>O'chirilgan joriy balanslar yig'indisi.</summary>
    public long BalanceRemoved { get; set; }

    /// <summary>O'chirilgan umr bo'yi ishlab topilgan coinlar yig'indisi.</summary>
    public long EarnedRemoved { get; set; }

    public int TransactionsRemoved { get; set; }

    [ForeignKey(nameof(CreatedBy))] public long? CreatedById { get; set; }

    [DeleteBehavior(DeleteBehavior.SetNull)] public User? CreatedBy { get; set; }
}
