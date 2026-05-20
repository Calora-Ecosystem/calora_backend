using Core.Entities.Course.Enum;
using Core.Enums;

namespace Core.Services.Course.Workout.Contracts;

public record ComputationShortDto
{
    public long? Id { get; set; }
    public EnumActivityLevel Activity { get; set; }
    public EnumComputationType ComputationType { get; set; }
    public double Value { get; set; }
    public double Kcal { get; set; }
}