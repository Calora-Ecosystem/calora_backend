using System.ComponentModel.DataAnnotations.Schema;
using BRB.Core.Common.Models.Base;
using Microsoft.EntityFrameworkCore;

namespace Core.Entities.Auth;

/// <summary>
/// Premium bo'lmagan userning bepul AI (rasm skan + ovoz) limiti.
/// Limit = <c>AiQuotaConfig.FreeLimit</c> + <see cref="BonusLimit"/> (marketplace'dan olingan).
/// Faqat muvaffaqiyatli (bo'sh bo'lmagan) natija <see cref="Used"/> ni oshiradi.
/// </summary>
[Index(nameof(UserId), IsUnique = true)]
public class UserAiQuota : AuditableModelBase<long>
{
    [ForeignKey(nameof(User))] public long UserId { get; set; }
    public int Used { get; set; }
    public int BonusLimit { get; set; }

    public User User { get; set; } = null!;
}
