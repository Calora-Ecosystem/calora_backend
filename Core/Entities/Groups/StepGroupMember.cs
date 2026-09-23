using System.ComponentModel.DataAnnotations.Schema;
using BRB.Core.Common.Models.Base;
using Core.Entities.Auth;
using Microsoft.EntityFrameworkCore;

namespace Core.Entities.Groups;

[Index(nameof(GroupId), nameof(UserId), IsUnique = true)]
[Index(nameof(UserId))]
public class StepGroupMember : AuditableModelBase<long>
{
    [ForeignKey(nameof(Group))] public long GroupId { get; set; }
    [ForeignKey(nameof(User))] public long UserId { get; set; }

    public StepGroup Group { get; set; } = null!;
    public User User { get; set; } = null!;
}
