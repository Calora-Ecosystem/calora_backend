using Core.Enums;
using Swashbuckle.AspNetCore.Annotations;

namespace Core.Services.FoodService.Contracts.FoodDtos;

public class UpdateDailyMenuDto
{
    [SwaggerSchema("New weight in GR")] public int WeightInGr { get; set; }

    [SwaggerSchema("Optional: move the item to another menu (Breakfast/Lunch/Dinner/Snack)")]
    public EnumMenu? Menu { get; set; }

    [SwaggerSchema("Optional: change the date of the logged item")]
    public DateTime? Date { get; set; }
}
