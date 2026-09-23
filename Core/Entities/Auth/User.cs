using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BRB.Core.Common.Models.Base;
using Core.Entities.Billing;
using Microsoft.EntityFrameworkCore;

namespace Core.Entities.Auth;

[Index(nameof(Email))]
[Index(nameof(ReferralCode), IsUnique = true)]
public class User : SoftDeletableAndAuditableModelBase<long>
{
    [MaxLength(300)] public string Name { get; set; } = null!;
    [MaxLength(100)] public string? Email { get; set; } = null!;
    [MaxLength(50)] public string? Phone { get; set; }
    [MaxLength(64)] public string? Password { get; set; } = null!;
    [MaxLength(50)] public string? RToken { get; set; }
    public DateTime RTokenExpireAt { get; set; }
    [Column(TypeName = "jsonb")] public List<string> Roles { get; set; } = null!;

    /// <summary>Do'st taklif qilish kodi; birinchi so'rovda lazy generatsiya qilinadi.</summary>
    [MaxLength(20)] public string? ReferralCode { get; set; }

    public UserExtra? Extra { get; set; } = null!;
    public Subscription? Subscription { get; set; } = null!;
}