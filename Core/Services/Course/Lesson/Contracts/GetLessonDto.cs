using BRB.Core.Common.Models;

namespace Core.Services.Course.Lesson.Contracts;

public record GetLessonDto
{
    public long Id { get; set; }
    public long CourseId { get; set; }
    public TimeSpan Duration { get; set; }
    public bool IsFree { get; set; }
    public MultiLanguageField Title { get; set; } = null!;
    public MultiLanguageField Description { get; set; } = null!;
    public decimal Order { get; set; }
}