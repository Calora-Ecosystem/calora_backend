using System.ComponentModel.DataAnnotations.Schema;
using BRB.Core.Common.Models.Base;
using Core.Entities.Auth;
using Core.Enums;
using Microsoft.EntityFrameworkCore;

namespace Core.Entities.FoodEntites;

public class DailyMenu : ModelBase<long>
{
    [ForeignKey(nameof(User))] public long UserId { get; set; }
    public DateTime Date { get; set; }
    public EnumMenu Menu { get; set; }
    public int Weight { get; set; }
    [ForeignKey(nameof(Food))] public long FoodId { get; set; }

    public User User { get; set; } = null!;

    [DeleteBehavior(DeleteBehavior.Restrict)]
    public Food Food { get; set; } = null!;
}