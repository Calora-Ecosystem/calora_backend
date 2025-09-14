using Core.Enums;

namespace Core.Services.User.Contracts;

public record GetNormDto(EnumMetrics Metric, double Value);