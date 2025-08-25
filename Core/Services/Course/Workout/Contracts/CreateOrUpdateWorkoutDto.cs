using Core.Services.Course.Common;

namespace Core.Services.Course.Workout.Contracts;

public class CreateOrUpdateWorkoutDto: BaseCreateOrUpdateDto
{
    public long CourseId { get; set; }
    public bool HasRest { get; set; }
}