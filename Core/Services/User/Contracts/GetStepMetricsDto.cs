using Swashbuckle.AspNetCore.Annotations;

namespace Core.Services.User.Contracts;

public record GetStepMetricsDto(
    long UserId,
    double Foots,
    [SwaggerSchema("Km")] double Distance,
    double Kcal,
    [SwaggerSchema("Hour")] double Duration);