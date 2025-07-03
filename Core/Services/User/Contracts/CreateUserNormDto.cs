using Core.Enums;

namespace Core.Services.User.Contracts;

public class CreateUserNormDto
{
    public EnumMetrics Metric { get; set; }
    public double Value { get; set; }
}

public class UpdateUserNormDto
{
    public long Value { get; set; }
}
