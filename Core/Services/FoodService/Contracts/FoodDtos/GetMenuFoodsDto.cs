using BRB.Core.Common.Models;
using Core.Entities.FoodEntites;
using Core.Enums;
using Core.Services.User.Contracts;

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
    public IEnumerable<GetNormDto> Metrics { get; set; } = null!;
    public long? UserId { get; set; }
    public int Weight { get; set; }
}