using System.ComponentModel.DataAnnotations.Schema;
using BRB.Core.Common.Models.Base;
using Core.Entities.Auth;
using Core.Entities.Crm.Enum;
using Microsoft.EntityFrameworkCore;

namespace Core.Entities.Crm;

[Index(nameof(Priority))]
[Index(nameof(SubscriptionOpenedCount))]
public class Lead : ModelBase<long>
{
    [ForeignKey(nameof(User))]
    public long UserId { get; set; }
    public bool IsRegistered { get; set; }
    public uint SubscriptionOpenedCount { get; set; }
    public bool Purchased { get; set; }
    public EnumLeadPriority Priority { get; set; }
    public DateTime LastActivity { get; set; }
    public User User { get; set; } = null!;
}