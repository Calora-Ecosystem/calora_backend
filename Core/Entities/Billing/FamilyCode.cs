using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BRB.Core.Common.Models.Base;
using Core.Entities.Auth;
using Microsoft.EntityFrameworkCore;

namespace Core.Entities.Billing;

/// <summary>
/// Oilaviy tarif: oilaviy order to'langanda xaridorga ikkinchi odam uchun bitta kod beriladi.
/// Kodni boshqa user kiritsa (<c>FamilyService.Redeem</c>) unga <see cref="Months"/> oy premium
/// beriladi. <see cref="OrderId"/> unique — bir to'lov uchun bitta kod (provayder to'lovni qayta
/// yuborsa ham).
/// </summary>
[Index(nameof(Code), IsUnique = true)]
[Index(nameof(OrderId), IsUnique = true)]
[Index(nameof(OwnerId), nameof(CreatedAt))]
public class FamilyCode : AuditableModelBase<long>
{
    /// <summary>Oilaviy tarifni sotib olgan user.</summary>
    [ForeignKey(nameof(Owner))] public long OwnerId { get; set; }

    [ForeignKey(nameof(Order))] public long OrderId { get; set; }

    [MaxLength(20)] public string Code { get; set; } = null!;

    /// <summary>Kod beradigan premium muddati (oilaviy tarif muddati bilan bir xil).</summary>
    public int Months { get; set; }

    /// <summary>Kod shu vaqtgacha faollashtirilishi kerak.</summary>
    public DateTime ExpireAt { get; set; }

    [ForeignKey(nameof(RedeemedBy))] public long? RedeemedById { get; set; }
    public DateTime? RedeemedAt { get; set; }

    public User Owner { get; set; } = null!;

    [DeleteBehavior(DeleteBehavior.Restrict)]
    public Order Order { get; set; } = null!;

    [DeleteBehavior(DeleteBehavior.SetNull)]
    public User? RedeemedBy { get; set; }
}
