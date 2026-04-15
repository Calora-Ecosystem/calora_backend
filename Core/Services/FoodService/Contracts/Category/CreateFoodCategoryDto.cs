using BRB.Core.Common.Models;

namespace Core.Services.FoodService.Contracts.Category;

public class CreateFoodCategoryDto
{
    public MultiLanguageField Name { get; set; } = null!;
    public string CoverUrl { get; set; } = null!;
}