using System.ComponentModel.DataAnnotations.Schema;
using BRB.Core.Common.Models.Base;
using Core.Enums;

namespace Core.Entities.Course;

public class ExerciseMetric : ModelBase<long>
{
    [ForeignKey(nameof(Exercise))] public long ExerciseId { get; set; }
    public EnumMetrics Metric { get; set; }
    public double Value { get; set; }

    public Exercise Exercise { get; set; } = null!;
}