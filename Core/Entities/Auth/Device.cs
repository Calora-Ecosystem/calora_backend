using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BRB.Core.Common.Models.Base;
using Microsoft.EntityFrameworkCore;

namespace Core.Entities.Auth;

[Index(nameof(Key))]
public class Device : AuditableModelBase<long>
{
    [ForeignKey(nameof(User))] public long UserId { get; set; }

    [MaxLength(150)] public string Key { get; set; } = null!;

    public User User { get; set; } = null!;
}