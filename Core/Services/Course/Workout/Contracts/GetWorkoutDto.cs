using BRB.Core.Common.Models;
using Core.Entities.Course;

namespace Core.Services.Course.Workout.Contracts;

public record GetWorkoutDto
{
    public long Id { get; set; }
    public long CourseId { get; set; }
    public MultiLanguageField Title { get; set; } = null!;
    public bool HasRest { get; set; }
    public int TotalItems { get; set; }
    public int DoneItems { get; set; }
    public bool IsDone { get; set; }
    public double TotalDurationInMin { get; set; }
    public IEnumerable<GetWorkoutMetricDto> TotalMetrics { get; set; } = null!;
    public decimal Order { get; set; }
    public double TotalKcal { get; set; }
}