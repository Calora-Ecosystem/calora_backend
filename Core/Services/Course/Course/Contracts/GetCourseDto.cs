using BRB.Core.Common.Models;
using Core.Enums;

namespace Core.Services.Course.Course.Contracts;

public record GetCourseDto
{
    public long Id { get; set; }
    public MultiLanguageField Title { get; set; } = null!;
    public MultiLanguageField Description { get; set; } = null!;
    public EnumGender? Gender { get; set; }
    public EnumCourseType Type { get; set; }
    public int Total { get; set; }
}