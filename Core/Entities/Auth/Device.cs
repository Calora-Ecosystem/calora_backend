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

    [MaxLength(150)] public string Name { get; set; } = null!;
    public string? FcmToken { get; set; }
    public bool IsActive { get; set; } = false;
    public User User { get; set; } = null!;
}