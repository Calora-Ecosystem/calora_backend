using BRB.Core.Common.Models;

namespace Core.Services.FoodService.Contracts.FoodDtos;

public record GetAllFoodsQuery : DataQueryRequest
{
    public bool Latest { get; set; } = false;
    public bool? IsUserFood { get; set; }
    public bool? IsFavourite { get; set; }
}