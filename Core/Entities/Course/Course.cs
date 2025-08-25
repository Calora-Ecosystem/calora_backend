using Core.Enums;

namespace Core.Entities.Course;

public class Course : BaseItem
{
    public long Price { get; set; }
    public EnumCourseType Type { get; set; }
    public EnumGender? Gender { get; set; }

    public ICollection<Lesson> Lessons { get; set; } = null!;
    public ICollection<Workout> Workouts { get; set; } = null!;
}