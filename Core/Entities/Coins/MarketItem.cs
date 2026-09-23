using System.ComponentModel.DataAnnotations;
using BRB.Core.Common.Models.Base;
using Core.Entities.Coins.Enum;
using Microsoft.EntityFrameworkCore;

namespace Core.Entities.Coins;

/// <summary>
/// Coin marketplace katalogi (SuperAdmin boshqaradi). <see cref="Title"/> va
/// <see cref="Subtitle"/> mobile lokalizatsiya kalitlari (masalan <c>mi_premium_7</c>).
/// </summary>
[Index(nameof(IsActive), nameof(SortOrder))]
public class MarketItem : AuditableModelBase<long>
{
    [MaxLength(100)] public string Title { get; set; } = null!;
    [MaxLength(100)] public string? Subtitle { get; set; }
    public long PriceCoins { get; set; }
    public EnumMarketCategory Category { get; set; }
    public EnumMarketRewardType RewardType { get; set; }
    public long RewardValue { get; set; }
    public bool IsPopular { get; set; }
    public bool IsActive { get; set; }
    public int SortOrder { get; set; }
}
