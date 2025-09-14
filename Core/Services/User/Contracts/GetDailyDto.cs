using Core.Enums;

namespace Core.Services.User.Contracts;

public record GetDailyDto(DateTime Date, EnumMetrics Metric, double Value);