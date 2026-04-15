using BRB.Core.Common.Models;
using Core.Entities.Course;

namespace Core.Services.Course.Workout.Contracts;

public record GetExerciseDto
{
    public long Id { get; set; }
    public long WorkoutId { get; set; }
    public MultiLanguageField Title { get; set; } = null!;
    public MultiLanguageField Description { get; set; } = null!;
    public Asset[] Assets { get; set; } = null!;
    public TimeSpan Duration { get; set; }
    public bool IsDone { get; set; }
    public decimal Order { get; set; }
}