using System.ComponentModel.DataAnnotations;
using BRB.Core.Common.Models;
using Core.Enums;

namespace Core.Services.FoodService.Contracts.FoodDtos;

public class CreateFoodDto
{
    public long? CategoryId { get; set; }
    public MultiLanguageField Name { get; set; } = null!;
    public string? CoverUrl { get; set; }
    [MaxLength(500)] public string? Description { get; set; }
    public List<FoodMetricDto> Metrics { get; set; } = default!;
}

public class CreateUserFood : CreateFoodDto
{
    [Required] public long UserId { get; set; }
}

public class UpdateFoodDto : CreateFoodDto
{
    public long? UserId { get; set; }
}

public class FoodMetricDto
{
    public EnumMetrics Metric { get; set; }
    public double Value { get; set; }
}