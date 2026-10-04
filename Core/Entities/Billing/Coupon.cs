using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BRB.Core.Common.Models.Base;
using Core.Entities.Auth;
using Microsoft.EntityFrameworkCore;

namespace Core.Entities.Billing;

[Index(nameof(Code), IsUnique = true)]
[Index(nameof(CreatedByUserId))]
public class Coupon : AuditableModelBase<long>
{
    [MaxLength(50)] public string Code { get; set; } = null!;
    [Column(TypeName = "jsonb")] public List<long>? AllowedUserIds { get; set; }
    public int Usages { get; set; }
    public bool OneTime { get; set; }
    public bool IsActive { get; set; }
    public long Amount { get; set; }
    public DateTime? ExpireAt { get; set; }

    /// <summary>Kuponni yaratgan foydalanuvchi (masalan oilaviy tarifni xarid qilgan shaxs).</summary>
    [ForeignKey(nameof(CreatedByUser))]
    public long? CreatedByUserId { get; set; }
    
    public User? CreatedByUser { get; set; }
}