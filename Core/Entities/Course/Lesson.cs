using System.ComponentModel.DataAnnotations.Schema;

namespace Core.Entities.Course;

public class Lesson : BaseItem
{
    [ForeignKey(nameof(Course))] public long CourseId { get; set; }

    public bool IsFree { get; set; }
    public TimeSpan Duration { get; set; }

    public Course Course { get; set; } = null!;
}