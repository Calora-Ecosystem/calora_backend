using Core.Enums;

namespace Core.Services.User.Contracts;

public record GetDailyDto
{
    public DateTime Date { get; init; }
    public EnumMetrics Metric { get; init; }
    public double Value { get; init; }
}