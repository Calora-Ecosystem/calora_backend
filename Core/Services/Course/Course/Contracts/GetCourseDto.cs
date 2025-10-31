using BRB.Core.Common.Models;
using Core.Entities.Course;
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
    public decimal Order { get; set; }
    public Asset[] Assets { get; set; } = null!;
    public long Price { get; set; }
}