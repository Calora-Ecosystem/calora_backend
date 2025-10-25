using Core.Enums;

namespace Core.Services.Course.Workout.Contracts;

public record GetWorkoutMetricDto
{
    public EnumMetrics Metric { get; set; }
    public double Sum { get; set; }
}