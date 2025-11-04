using BRB.Core.Common.Models;
using Core.Enums;
using Microsoft.EntityFrameworkCore;

namespace Core.Entities.Course;

[Index(nameof(Order))]
public class Course : BaseItem
{
    public long Price { get; set; }
    public EnumCourseType Type { get; set; }
    public EnumGender? Gender { get; set; }
    public MultiLanguageField? Info { get; set; } = null!;

    public ICollection<Lesson> Lessons { get; set; } = null!;
    public ICollection<Workout> Workouts { get; set; } = null!;
}