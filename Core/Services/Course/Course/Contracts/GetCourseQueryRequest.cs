using BRB.Core.Common.Models;
using Core.Enums;

namespace Core.Services.Course.Course.Contracts;

public record GetCourseQueryRequest : DataQueryRequest
{
    public EnumGender? Gender { get; set; }
}