using Core.Enums;
using Core.Services.User.Contracts;

namespace Core.Services.FoodService.Contracts.FoodDtos;

public record SummaryDto
{
    public GetNormDto KcalNorm { get; set; } = null!;
    public Dictionary<EnumMenu, NutrientSummaryDto> NutrientsNorm { get; set; } = null!;
    public Dictionary<EnumMenu, NutrientSummaryDto> Nutrients { get; set; } = null!;
    public double SumKcal { get; set; }
    public DateTime? Date { get; set; }
}