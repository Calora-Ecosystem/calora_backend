using BRB.Core.Common.Models.Base;
using Core.Entities.Course.Enum;
using Core.Enums;
using Microsoft.EntityFrameworkCore;

namespace Core.Entities.Course;

[Index(nameof(EntityId))]
public class Computation : ModelBase<long>
{
    public long EntityId { get; set; }
    public EnumEntityType Type { get; set; }
    public EnumActivityLevel Level { get; set; }
    public EnumComputationType ComputationType { get; set; }
    public double Value { get; set; }
}