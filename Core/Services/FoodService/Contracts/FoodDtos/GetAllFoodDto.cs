using BRB.Core.Common.Models;
using Core.Services.User.Contracts;

namespace Core.Services.FoodService.Contracts.FoodDtos;

public record GetAllFoodDto
{
    public long Id { get; set; }
    public MultiLanguageField Name { get; set; } = null!;
    public long CategoryId { get; set; }
    public MultiLanguageField CategoryName { get; set; } = null!;
    public string? CoverUrl { get; set; } = null!;
    public IEnumerable<GetNormDto> Metrics { get; set; } = null!;
    public bool IsUserFood { get; set; }
}