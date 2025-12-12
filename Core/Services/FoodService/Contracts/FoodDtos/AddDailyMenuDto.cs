using Core.Enums;
using Swashbuckle.AspNetCore.Annotations;

namespace Core.Services.FoodService.Contracts.FoodDtos;

public class AddDailyMenuDto
{
    public EnumMenu Menu { get; set; }
    public DateTime? Date { get; set; }
    public long FoodId { get; set; }

    [SwaggerSchema("Weight in GR")] public int WeightInGr { get; set; }
}