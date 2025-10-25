using BRB.Core.Common.Models;

namespace Core.Services.Course.Workout.Contracts;

public record GetExerciseDto
{
    public long Id { get; set; }
    public long WorkoutId { get; set; }
    public MultiLanguageField Title { get; set; } = null!;
    public MultiLanguageField Description { get; set; } = null!;
    public string[] Assets { get; set; } = null!;
    public TimeSpan Duration { get; set; }
    public bool IsDone { get; set; }
}