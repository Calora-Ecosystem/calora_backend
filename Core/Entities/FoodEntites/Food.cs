using System.ComponentModel.DataAnnotations.Schema;
using BRB.Core.Common.Models;
using BRB.Core.Common.Models.Base;
using Core.Entities.Auth;

namespace Core.Entities.FoodEntites;

public class Food : ModelBase<long>
{
    /// <summary>
    /// if is set, it is user's food
    /// </summary>
    public long? UserId { get; set; }

    [ForeignKey(nameof(FoodCategory))] public long CategoryId { get; set; }
    public MultiLanguageField Name { get; set; } = null!;
    public string? CoverUrl { get; set; }

    public FoodCategory Category { get; set; } = default!;

    public User? User { get; set; }
    public List<FoodMetrics> Metrics { get; set; } = default!;
}