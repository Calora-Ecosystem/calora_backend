using System.ComponentModel.DataAnnotations.Schema;
using BRB.Core.Common.Models.Base;
using Core.Enums;

namespace Core.Entities.FoodEntites;

public class FoodMetrics : ModelBase<long>
{
    [ForeignKey(nameof(Food))] public long FoodId { get; set; }
    public EnumMetrics Metric { get; set; }
    public double Value { get; set; }

    public Food Food { get; set; } = null!;
}