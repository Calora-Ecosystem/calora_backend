using Core.Entities.Course;
using Core.Services.Course.Common;

namespace Core.Services.Course.Lesson.Contracts;

public class CreateOrUpdateLessonDto : BaseCreateOrUpdateDto
{
    public long CourseId { get; set; }
    public bool IsFree { get; set; }
    public TimeSpan Duration { get; set; }
    public Asset[] Assets { get; set; } = null!;
}