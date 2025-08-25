using Core.Enums;
using Core.Services.Course.Common;

namespace Core.Services.Course.Workout.Contracts;

public class CreateOrUpdateExerciseDto : BaseCreateOrUpdateDto
{
    public long WorkoutId { get; set; }
    public TimeSpan Duration { get; set; }
    public string[] Assets { get; set; } = null!;
    public List<MetricsUpdateOrCreateDto> Metrics { get; set; } = null!;
}

public class MetricsUpdateOrCreateDto
{
    public EnumMetrics Metric { get; set; }
    public double Value { get; set; }
}