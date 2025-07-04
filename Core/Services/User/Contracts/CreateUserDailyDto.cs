using Core.Enums;

namespace Core.Services.User.Contracts;

public class CreateUserDailyDto
{
    public EnumMetrics Metric { get; set; }
    public double Value { get; set; }
    public DateTime Date { get; set; }
}

public class UpdateUserDailyDto
{
    public double Value { get; set; }
}
