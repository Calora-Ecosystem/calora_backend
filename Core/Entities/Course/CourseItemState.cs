using BRB.Core.Common.Models.Base;
using Core.Entities.Course.Enum;
using Microsoft.EntityFrameworkCore;

namespace Core.Entities.Course;

[Index(nameof(UserId), nameof(EntityId), nameof(Type))]
public class CourseItemState : AuditableModelBase<long>
{
    public long EntityId { get; set; }
    public long UserId { get; set; }
    public EnumHistoryEntityType Type { get; set; }
}