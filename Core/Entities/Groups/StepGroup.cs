using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BRB.Core.Common.Models.Base;
using Core.Entities.Auth;
using Microsoft.EntityFrameworkCore;

namespace Core.Entities.Groups;

/// <summary>
/// Userlar o'zlari yaratadigan qadam guruhi (do'stlar bilan musobaqa).
/// Boshqalar <see cref="InviteCode"/> orqali qo'shiladi.
/// </summary>
[Index(nameof(InviteCode), IsUnique = true)]
[Index(nameof(OwnerId))]
public class StepGroup : AuditableModelBase<long>
{
    [MaxLength(100)] public string Name { get; set; } = null!;
    [MaxLength(20)] public string InviteCode { get; set; } = null!;
    [ForeignKey(nameof(Owner))] public long OwnerId { get; set; }

    [DeleteBehavior(DeleteBehavior.Restrict)] public User Owner { get; set; } = null!;
    public List<StepGroupMember> Members { get; set; } = null!;
}
