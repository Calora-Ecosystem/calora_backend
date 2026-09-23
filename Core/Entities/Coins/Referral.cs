using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BRB.Core.Common.Models.Base;
using Core.Entities.Auth;
using Microsoft.EntityFrameworkCore;

namespace Core.Entities.Coins;

/// <summary>
/// Taklif (referral) yozuvi — yangi user ro'yxatdan o'tgach do'stining kodini
/// tasdiqlaganda yaratiladi. Har bir user faqat bir marta taklif qilingan
/// bo'lishi mumkin (<see cref="ReferredUserId"/> unique).
/// </summary>
[Index(nameof(ReferredUserId), IsUnique = true)]
[Index(nameof(ReferrerId), nameof(QualifiedAt))]
public class Referral : AuditableModelBase<long>
{
    [ForeignKey(nameof(Referrer))] public long ReferrerId { get; set; }
    [ForeignKey(nameof(ReferredUser))] public long ReferredUserId { get; set; }

    /// <summary>Tasdiqlangan kod (qaysi ulashishdan kelgani).</summary>
    [MaxLength(20)] public string? Code { get; set; }

    /// <summary>
    /// Taklif qilingan user ilovaga to'liq kirgan (onboarding/profilni tugatgan) vaqt.
    /// Faqat shundan keyin taklif qiluvchining premium hisobiga qo'shiladi.
    /// </summary>
    public DateTime? QualifiedAt { get; set; }

    /// <summary>Taklif qiluvchiga berilgan coin.</summary>
    public long ReferrerReward { get; set; }

    /// <summary>Taklif qilinganga berilgan coin.</summary>
    public long ReferredReward { get; set; }

    /// <summary>Taklif qilingan user referral chegirmasini ishlatgan vaqt (bir marta).</summary>
    public DateTime? DiscountUsedAt { get; set; }

    public long? DiscountOrderId { get; set; }

    [DeleteBehavior(DeleteBehavior.Restrict)] public User Referrer { get; set; } = null!;
    [DeleteBehavior(DeleteBehavior.Restrict)] public User ReferredUser { get; set; } = null!;
}
