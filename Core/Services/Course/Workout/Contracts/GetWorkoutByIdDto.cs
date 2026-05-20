using BRB.Core.Common.Models;
using Core.Entities.Course;

namespace Core.Services.Course.Workout.Contracts;

public record GetWorkoutByIdDto
{
    public long Id { get; set; }
    public MultiLanguageField Title { get; set; } = null!;
    public MultiLanguageField Description { get; set; } = null!;
    public bool HasRest { get; set; }
    public long CourseId { get; set; }
    public Asset[] Assets { get; set; } = null!;
    public decimal Order { get; set; }
}