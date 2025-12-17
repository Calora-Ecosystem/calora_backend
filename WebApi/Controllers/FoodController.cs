using BRB.Core.Common.Exceptions;
using BRB.Core.Common.Models;
using Core;
using Core.Entities.FoodEntites;
using Core.Enums;
using Core.Services.Ai.Contracts;
using Core.Services.FoodService;
using Core.Services.FoodService.Contracts.Category;
using Core.Services.FoodService.Contracts.FoodDtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ResultWrapper.Library;
using WebCore.Controller;

namespace WebApi.Controllers;

[ApiController]
[Route("food")]
[RoleAuthorize(EnumRole.User)]
public class FoodController(FoodService service) : AuthorizedController
{
    #region Food

    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(WrapperGeneric<IEnumerable<GetAllFoodDto>>), 200)]
    public async Task<Wrapper> GetAllFoods([FromQuery] DataQueryRequest q, bool latest = false) =>
        await service.GetAllFoods(this.HasAuthorized ? this.UserId : null, q, latest);

    [HttpGet("favourites")]
    [ProducesResponseType(typeof(WrapperGeneric<IEnumerable<GetAllFoodDto>>), 200)]
    public async Task<Wrapper> GetFavouriteFoods([FromQuery] DataQueryRequest q) =>
        await service.GetFavouriteFoods(this.UserId, q);

    [HttpPost("favourites/toggle/{foodId:long:min(1)}")]
    public async Task<Wrapper> ToggleFavouriteFood(long foodId)
    {
        await service.ToggleFavouriteFood(this.UserId, foodId);
        return 200;
    }

    [HttpGet("{foodId:long:min(1)}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(WrapperGeneric<FoodDto>), 200)]
    public async Task<Wrapper> GetFoodById(long foodId) =>
        (await service.GetFoodById(foodId, this.HasAuthorized ? this.UserId : null), 200);

    [HttpPost]
    public async Task<Wrapper> CreateUserFood(CreateUserFood dto) => (await service.CreateFood(this.UserId, dto), 200);

    [HttpPost("general")]
    [RoleAuthorize(EnumRole.SuperAdmin)]
    public async Task<Wrapper> CreateGeneralFood(CreateFoodDto dto) =>
        (await service.CreateFood(this.UserId, dto), 200);

    [HttpPut("{foodId:long:min(1)}")]
    public async Task<Wrapper> UpdateFood(long foodId, UpdateFoodDto dto) =>
        (await service.UpdateFood(foodId, dto), 200);


    [HttpPost("recognization")]
    public async Task<WrapperGeneric<IEnumerable<FoodResultDto>>> RecognizeFood([FromForm] RecognizeFoodDto dto) =>
        (await service.RecognizeFood(dto), 200);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="foodId"></param>
    /// <returns></returns>
    /// ToDo: Check for USER role
    [HttpDelete("user-food/{foodId:long:min(1)}")]
    public async Task<Wrapper> RemoveUserFood(long foodId) => (await service.RemoveUserFood(foodId, this.UserId), 200);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="foodId"></param>
    /// <returns></returns>
    /// ToDo: Check for ADMIN role
    [HttpDelete("{foodId:long:min(1)}")]
    [RoleAuthorize(EnumRole.SuperAdmin)]
    public async Task<Wrapper> RemoveFood(long foodId) => (await service.RemoveFood(foodId), 200);

    #endregion

    #region Category

    [HttpGet("categories")]
    [RoleAuthorize(EnumRole.User)]
    [ProducesResponseType(typeof(WrapperGeneric<IEnumerable<FoodCategory>>), 200)]
    public async Task<Wrapper> GetAllCategories([FromQuery] DataQueryRequest q) => await service.GetAllCategory(q);

    [HttpPost("categories")]
    public async Task<Wrapper> CreateCategory(CreateFoodCategoryDto dto) => (await service.CreateCategory(dto), 200);

    [HttpDelete("categories/{id:long:min(1)}")]
    public async Task<Wrapper> RemoveCategory(long categoryId) => (await service.RemoveCategory(categoryId), 200);

    #endregion

    #region Menu

    [HttpGet("menu")]
    [RoleAuthorize(EnumRole.User)]
    [ProducesResponseType(typeof(WrapperGeneric<IEnumerable<GetMenuFoodsDto>>), 200)]
    public async Task<Wrapper> GetMenuFoods([FromQuery] DataQueryRequest q, [FromQuery] EnumMenu? menu,
        [FromQuery] DateTime? date) =>
        await service.GetMenuFoods(this.UserId, menu, date, q);

    [HttpPost("menu")]
    public async Task<Wrapper> AddMenuItem(AddDailyMenuDto dto) =>
        (await service.AddDailyMenuItem(this.UserId, dto), 200);

    [HttpDelete("menu/{itemId:long:min(1)}")]
    public async Task<Wrapper> RemoveMenuItem(long itemId) => (await service.RemoveMenuItem(this.UserId, itemId), 200);

    #endregion

    #region Summary

    /// <summary>
    /// User nutrition summary
    /// </summary>
    /// <param name="date">if is null, now</param>
    /// <returns></returns>
    [HttpGet("summary")]
    [RoleAuthorize(EnumRole.User)]
    [ProducesResponseType(typeof(WrapperGeneric<SummaryDto>), 200)]
    public async Task<Wrapper> Summary(DateTime? date) => (await service.Summary(this.UserId, date), 200);

    #endregion
}