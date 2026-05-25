using BRB.Core.Common.Models.Base;
using Core.Enums;
using Microsoft.EntityFrameworkCore;

namespace Core.Entities.Course;

[Index(nameof(EntityId), nameof(Level))]
public class WorkoutComputationIndex : ModelBase<long>
{
    public long EntityId { get; set; }
    public TimeSpan TotalDuration { get; set; }
    public int TotalCounts { get; set; }
    public EnumActivityLevel Level { get; set; }
    public double TotalKcal { get; set; }
}