using System.Text.Json.Serialization;
using Core.Enums;

namespace Core.Services.Ai.Contracts;

public class FoodResultDto
{
    public string? Name { get; set; } = null!;
    public double? Weight { get; set; }
    public List<MetricResult> Metrics { get; set; } = null!;
}

public class MetricResult
{
    public EnumMetrics Metric { get; set; }
    public double Value { get; set; }
}