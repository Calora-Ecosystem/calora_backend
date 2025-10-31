using Core.Enums;

namespace Core.Services.User.Contracts;

public record UserMetricSummaryDto
{
    public double Target { get; set; }
    public double Progress { get; set; }
    public EnumMetrics Metric { get; set; }
}