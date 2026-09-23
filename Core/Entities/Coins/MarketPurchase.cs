using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BRB.Core.Common.Models.Base;
using Core.Entities.Auth;
using Core.Entities.Coins.Enum;
using Microsoft.EntityFrameworkCore;

namespace Core.Entities.Coins;

/// <summary>
/// Marketplace xaridi. Narx va mukofot xarid paytidagi holatda saqlanadi
/// (katalog keyin o'zgarsa ham tarix buzilmaydi).
/// </summary>
[Index(nameof(UserId), nameof(CreatedAt))]
public class MarketPurchase : AuditableModelBase<long>
{
    [ForeignKey(nameof(User))] public long UserId { get; set; }
    [ForeignKey(nameof(MarketItem))] public long MarketItemId { get; set; }
    [MaxLength(100)] public string Title { get; set; } = null!;
    public long PriceCoins { get; set; }
    public EnumMarketRewardType RewardType { get; set; }
    public long RewardValue { get; set; }

    /// <summary>Coupon/Voucher turidagi xaridlar uchun berilgan kod.</summary>
    [MaxLength(50)] public string? Code { get; set; }

    public User User { get; set; } = null!;
    [DeleteBehavior(DeleteBehavior.Restrict)] public MarketItem MarketItem { get; set; } = null!;
}
