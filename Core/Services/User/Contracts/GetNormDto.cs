using Core.Enums;

namespace Core.Services.User.Contracts;

public record GetNormDto
{
    public long UserId { get; }
    public EnumMetrics Metric { get; }
    public double Value { get; set; }

    public GetNormDto(EnumMetrics metric, double value)
    {
        Metric = metric;
        Value = value;
    }

    public GetNormDto(long userId, EnumMetrics metric, double value)
    {
        Value = value;
        UserId = userId;
        Metric = metric;
    }
}