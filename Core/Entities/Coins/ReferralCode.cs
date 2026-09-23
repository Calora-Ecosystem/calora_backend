using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BRB.Core.Common.Models.Base;
using Core.Entities.Auth;
using Microsoft.EntityFrameworkCore;

namespace Core.Entities.Coins;

/// <summary>
/// Userning taklif kodi. Har ulashishda yangi kod yaratiladi (bir xil kod qayta-qayta
/// yuborilmaydi), eski kodlar ham amal qilaveradi — oldin yuborilgan xabar eskirmaydi.
/// </summary>
[Index(nameof(Code), IsUnique = true)]
[Index(nameof(UserId), nameof(CreatedAt))]
public class ReferralCode : AuditableModelBase<long>
{
    [ForeignKey(nameof(User))] public long UserId { get; set; }
    [MaxLength(20)] public string Code { get; set; } = null!;

    public User User { get; set; } = null!;
}
