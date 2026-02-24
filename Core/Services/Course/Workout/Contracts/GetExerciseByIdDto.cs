using BRB.Core.Common.Models;
using Core.Entities.Course;
using Core.Enums;

namespace Core.Services.Course.Workout.Contracts;

public record GetExerciseByIdDto
{
    public long Id { get; set; }
    public long WorkoutId { get; set; }
    public MultiLanguageField Title { get; set; } = null!;
    public MultiLanguageField Description { get; set; } = null!;
    public Asset[] Assets { get; set; } = null!;
    public TimeSpan Duration { get; set; }
    public decimal Order { get; set; }
    public List<ExerciseMetricDto> Metrics { get; set; } = null!;
}

public record ExerciseMetricDto
{
    public EnumMetrics Metric { get; set; }
    public double Value { get; set; }
}
