using System.ComponentModel.DataAnnotations.Schema;
using BRB.Core.Common.Models;
using BRB.Core.Common.Models.Base;

namespace Core.Entities.Course;

public class Workout : BaseItem
{
    // public MultiLanguageField Title { get; set; } = null!;
    [ForeignKey(nameof(Course))] public long CourseId { get; set; }

    public bool HasRest { get; set; } = false;
    public Course Course { get; set; } = null!;
    public ICollection<Exercise> Exercises { get; set; } = null!;
}