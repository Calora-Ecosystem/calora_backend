using BRB.Core.Common.Models;
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
public class FoodController(FoodService service) : AuthorizedController
{
    #region Food

    [HttpGet]
    [AllowAnonymous]
    public async Task<Wrapper> GetAllFoods([FromQuery] DataQueryRequest q) =>
        await service.GetAllFoods(this.HasAuthorized ? this.UserId : null, q);

    [HttpPost]
    public async Task<Wrapper> CreateFood(CreateFoodDto dto) => (await service.CreateFood(dto), 200);

    [HttpPut("{foodId:long:min(1)}")]
    public async Task<Wrapper> UpdateFood(long foodId, UpdateFoodDto dto) =>
        (await service.UpdateFood(foodId, dto), 200);

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
    public async Task<Wrapper> RemoveFood(long foodId) => (await service.RemoveFood(foodId), 200);

    #endregion

    #region Category

    [HttpGet("categories")]
    public async Task<Wrapper> GetAllCategories([FromQuery] DataQueryRequest q) => await service.GetAllCategory(q);

    [HttpPost("categories")]
    public async Task<Wrapper> CreateCategory(CreateFoodCategoryDto dto) => (await service.CreateCategory(dto), 200);

    [HttpDelete("categories/{id:long:min(1)}")]
    public async Task<Wrapper> RemoveCategory(long categoryId) => (await service.RemoveCategory(categoryId), 200);

    #endregion

    #region Menu

    [HttpGet("menu")]
    public async Task<Wrapper> GetMenuFoods([FromQuery] DataQueryRequest q) =>
        await service.GetMenuFoods(this.UserId, q);

    [HttpPost("menu")]
    public async Task<Wrapper> AddMenuItem(AddDailyMenuDto dto) =>
        (await service.AddDailyMenuItem(this.UserId, dto), 200);

    [HttpDelete("menu/{itemId:long:min(1)}")]
    public async Task<Wrapper> RemoveMenuItem(long itemId) => (await service.RemoveMenuItem(this.UserId, itemId), 200);

    #endregion
}