using System.ComponentModel.DataAnnotations.Schema;

namespace Core.Entities.Course;

public class Exercise : BaseItem
{
    [ForeignKey(nameof(Workout))] public long WorkoutId { get; set; }
    public TimeSpan Duration { get; set; }
    public Workout Workout { get; set; } = null!;
    public ICollection<ExerciseMetric> Metrics { get; set; } = null!;
}