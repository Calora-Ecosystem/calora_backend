using BRB.Core.Common.Models;

namespace Core.Services.FoodService.Contracts.Category;

public class CreateFoodCategoryDto
{
    public long? Id { get; set; }
    public MultiLanguageField Name { get; set; } = null!;
    public string CoverUrl { get; set; } = null!;
}