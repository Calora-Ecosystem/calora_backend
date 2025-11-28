using BRB.Core.Common.Models;
using Core.Entities.FoodEntites;
using Core.Enums;

namespace Core.Services.FoodService.Contracts.FoodDtos;

public record GetMenuFoodsDto
{
    public EnumMenu Menu { get; set; }
    public DateTime Date { get; set; }
    public long FoodId { get; set; }
    public MultiLanguageField FoodName { get; set; } = null!;
    public long CategoryId { get; set; }
    public MultiLanguageField CategoryName { get; set; } = null!;
    public string? CoverUrl { get; set; }
    public List<FoodMetrics> Metrics { get; set; } = null!;
    public long? UserId { get; set; }
    public MultiLanguageField Name { get; set; } = null!;
}