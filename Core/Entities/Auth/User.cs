using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BRB.Core.Common.Models.Base;
using Core.Entities.Billing;
using Microsoft.EntityFrameworkCore;

namespace Core.Entities.Auth;

[Index(nameof(Email))]
public class User : AuditableModelBase<long>
{
    [MaxLength(300)] public string Name { get; set; } = null!;
    [MaxLength(100)] public string Email { get; set; } = null!;
    [MaxLength(64)] public string? Password { get; set; } = null!;
    [MaxLength(50)] public string? RToken { get; set; }
    public DateTime RTokenExpireAt { get; set; }
    [ForeignKey(nameof(Subscription))] public long SubscriptionId { get; set; }
    [Column(TypeName = "jsonb")] public List<string> Roles { get; set; } = null!;

    public UserExtra? Extra { get; set; } = null!;
    public Subscription Subscription { get; set; } = null!;
}