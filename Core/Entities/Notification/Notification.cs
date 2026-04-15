using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BRB.Core.Common.Models.Base;
using Core.Entities.Auth;

namespace Core.Entities.Notification;

public class Notification : AuditableModelBase<long>
{
    [ForeignKey(nameof(User))] public long UserId { get; set; }
    [MaxLength(200)] public string Title { get; set; } = null!;
    [MaxLength(500)] public string? Description { get; set; } = null!;
    public bool HasRead { get; set; } = false;
    public User User { get; set; } = null!;
}