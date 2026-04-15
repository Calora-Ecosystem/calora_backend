using BRB.Core.Common.Models;
using Core.Services.User.Contracts;

namespace Core.Services.FoodService.Contracts.FoodDtos;

public record FoodDto
{
    public long Id { get; set; }
    public MultiLanguageField Name { get; set; } = null!;
    public long CategoryId { get; set; }
    public string? Description { get; set; }
    public MultiLanguageField CategoryName { get; set; } = null!;
    public string? CoverUrl { get; set; }
    public IEnumerable<GetNormDto> Metrics { get; set; } = null!;
    public bool IsUserFood { get; set; }
    public long? UserId { get; set; }
}