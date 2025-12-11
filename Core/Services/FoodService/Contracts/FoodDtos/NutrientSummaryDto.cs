using Core.Enums;

namespace Core.Services.FoodService.Contracts.FoodDtos;

public record NutrientSummaryDto
{
    public EnumMenu Menu { get; set; }
    public double Kcal { get; set; }
    public double Fat { get; set; }
    public double Protein { get; set; }
    public double Carb { get; set; }
    public double Weight { get; set; }
}