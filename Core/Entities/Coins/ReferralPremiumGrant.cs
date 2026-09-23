using System.ComponentModel.DataAnnotations.Schema;
using BRB.Core.Common.Models.Base;
using Core.Entities.Auth;
using Microsoft.EntityFrameworkCore;

namespace Core.Entities.Coins;

/// <summary>
/// Har <c>ReferralPremiumFriends</c> ta faol do'st uchun berilgan premium.
/// (ReferrerId, Milestone) unique — bir bosqich ikki marta berilmaydi (parallel so'rovlarda ham).
/// </summary>
[Index(nameof(ReferrerId), nameof(Milestone), IsUnique = true)]
public class ReferralPremiumGrant : AuditableModelBase<long>
{
    [ForeignKey(nameof(Referrer))] public long ReferrerId { get; set; }

    /// <summary>1 — birinchi 5 ta do'st, 2 — keyingi 5 ta va h.k.</summary>
    public int Milestone { get; set; }

    public int Days { get; set; }

    [DeleteBehavior(DeleteBehavior.Restrict)] public User Referrer { get; set; } = null!;
}
