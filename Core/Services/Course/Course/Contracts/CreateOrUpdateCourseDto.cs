using BRB.Core.Common.Models;
using Core.Entities.Course;
using Core.Enums;
using Core.Services.Course.Common;

namespace Core.Services.Course.Course.Contracts;

public class CreateOrUpdateCourseDto : BaseCreateOrUpdateDto
{
    public EnumCourseType Type { get; set; }
    public EnumGender? Gender { get; set; }
    public MultiLanguageField Info { get; set; } = null!;
    public Asset[] Assets { get; set; } = null!;
}