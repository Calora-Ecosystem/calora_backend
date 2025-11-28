using BRB.Core.Common.Exceptions;
using BRB.Core.Common.Extensions;
using BRB.Core.Common.Models;
using BRB.Core.EF.Attributes;
using BRB.Core.EF.Extensions;
using Core.Brokers.DbContext;
using Core.Entities.FoodEntites;
using Core.Enums;
using Core.Services.FoodService.Contracts.Category;
using Core.Services.FoodService.Contracts.FoodDtos;
using Core.Services.User;
using Core.Services.User.Contracts;
using Microsoft.EntityFrameworkCore;
using ResultWrapper.Library;

namespace Core.Services.FoodService;

[Injectable]
public class FoodService(AppDbContext dbContext)
{
    #region Category

    public async Task<Wrapper> GetAllCategory(DataQueryRequest q)
    {
        return await dbContext.FoodCategories
            // .Select(x => new
            // {
            //     x.Id,
            //     x.Name,
            //     x.CoverUrl
            // })
            .GetByDataQueryAsync(q);
    }

    public async Task<FoodCategory> CreateCategory(CreateFoodCategoryDto dto)
    {
        var category = dbContext.FoodCategories.Add(new FoodCategory()
        {
            CoverUrl = dto.CoverUrl,
            Name = dto.Name,
        }).Entity;
        await dbContext.SaveChangesAsync();

        return category;
    }

    public async Task<int> RemoveCategory(long categoryId)
    {
        return await dbContext.FoodCategories.Where(x => x.Id == categoryId)
            .ExecuteDeleteAsync();
    }

    #endregion

    #region Food

    public async Task<Wrapper> GetAllFoods(long? userId, DataQueryRequest q)
    {
        var queryable = dbContext.Foods.AsQueryable();

        if (userId is not null)
            queryable = queryable.Where(x => x.UserId == userId || x.UserId.HasValue == false);

        return await queryable
            .Select(x => new GetAllFoodDto
            {
                Id = x.Id, Name = x.Name, CategoryId = x.CategoryId,
                CategoryName = x.Category.Name,
                CoverUrl = x.CoverUrl,
                Metrics = x.Metrics.Select(foodMetrics => new GetNormDto(foodMetrics.Metric, foodMetrics.Value)),
                IsUserFood = x.UserId.HasValue
            })
            .GetByDataQueryAsync(q);
    }

    public async Task<FoodDto> GetFoodById(long foodId, long? userId)
    {
        var food = await dbContext.Foods
            .AsNoTracking()
            .Select(x => new FoodDto
            {
                Id = x.Id, Name = x.Name, CategoryId = x.CategoryId,
                Description = x.Description,
                CategoryName = x.Category.Name,
                CoverUrl = x.CoverUrl,
                Metrics = x.Metrics.Select(foodMetrics => new GetNormDto(foodMetrics.Metric, foodMetrics.Value)),
                IsUserFood = x.UserId.HasValue,
                UserId = x.UserId,
            })
            .FirstOrDefaultAsync(x => x.Id == foodId) ?? throw new NotFoundException("Food not found");

        if (userId.HasValue && food.IsUserFood && food.UserId != userId)
            throw new NotFoundException("Food not found");

        var metricsDict = new List<EnumMetrics>([EnumMetrics.Weight, EnumMetrics.Kcal, EnumMetrics.Carb, EnumMetrics.Fat, EnumMetrics.Protein]).ToDictionary(x => x, x => new GetNormDto(userId ?? 0, x, 0));

        food.Metrics.ForEach(x => metricsDict[x.Metric] = x);
        food.Metrics = metricsDict.Values;

        return food;
    }

    public async Task<Wrapper> GetFavouriteFoods(long userId, DataQueryRequest q)
    {
        return await dbContext
            .UserExtras
            .Where(x => x.UserId == userId)
            .SelectMany(x => x.FavouriteFoods)
            .Select(x => new GetAllFoodDto
            {
                Id = x.Id, Name = x.Name, CategoryId = x.CategoryId,
                CategoryName = x.Category.Name,
                CoverUrl = x.CoverUrl,
                Metrics = x.Metrics.Select(foodMetrics => new GetNormDto(foodMetrics.Metric, foodMetrics.Value)),
                IsUserFood = x.UserId.HasValue
            })
            .GetByDataQueryAsync(q);
    }

    public async Task<Food> CreateFood(CreateFoodDto dto)
    {
        await dbContext.FoodCategories.ExistsOrThrowsNotFoundException(dto.CategoryId);

        var food = dbContext.Foods.Add(new Food()
        {
            UserId = dto.UserId,
            CategoryId = dto.CategoryId,
            CoverUrl = dto.CoverUrl,
            Name = dto.Name,
            Description = dto.Description
        }).Entity;

        await dbContext.Transactional(async () =>
        {
            await dbContext.SaveChangesAsync();

            dbContext.FoodMetrics.AddRange(
                dto.Metrics.DistinctBy(x => x.Metric).Select(x => new FoodMetrics()
                {
                    FoodId = food.Id,
                    Metric = x.Metric,
                    Value = x.Value
                })
            );

            await dbContext.SaveChangesAsync();
        });

        return food;
    }

    public async Task<Food> UpdateFood(long foodId, UpdateFoodDto dto)
    {
        var food = await dbContext.Foods.GetByIdOrThrowsNotFoundException(foodId);
        await dbContext.FoodCategories.ExistsOrThrowsNotFoundException(dto.CategoryId);

        await dbContext.Transactional(async () =>
        {
            food.UserId = dto.UserId;
            food.CategoryId = dto.CategoryId;
            food.CoverUrl = dto.CoverUrl;
            food.Name = dto.Name;

            food = dbContext.Foods.Update(food).Entity;

            //clear food metrics
            await dbContext.FoodMetrics.Where(x => x.FoodId == food.Id).ExecuteDeleteAsync();

            dbContext.FoodMetrics.AddRange(
                dto.Metrics.DistinctBy(x => x.Metric).Select(x => new FoodMetrics()
                {
                    FoodId = food.Id,
                    Metric = x.Metric,
                    Value = x.Value
                })
            );

            await dbContext.SaveChangesAsync();
        });

        return food;
    }

    /// <summary>
    /// Remove for normal users
    /// </summary>
    /// <param name="foodId"></param>
    /// <param name="userId"></param>
    /// <returns></returns>
    public async Task<int> RemoveUserFood(long foodId, long userId)
    {
        return await dbContext.Foods.Where(x => x.Id == foodId && x.UserId == userId)
            .ExecuteDeleteAsync();
    }

    /// <summary>
    /// Remove for admins
    /// </summary>
    /// <param name="foodId"></param>
    /// <returns></returns>
    public async Task<int> RemoveFood(long foodId)
    {
        return await dbContext.Foods.Where(x => x.Id == foodId)
            .ExecuteDeleteAsync();
    }

    #endregion

    #region Menu

    public async Task<Wrapper> GetMenuFoods(long userId, DataQueryRequest q)
    {
        return await dbContext.DailyMenus
            .Where(x => x.UserId == userId)
            .Select(x => new GetMenuFoodsDto
            {
                Menu = x.Menu, Date = x.Date, FoodId = x.FoodId,
                FoodName = x.Food.Name,
                CategoryId = x.Food.CategoryId,
                CategoryName = x.Food.Category.Name,
                CoverUrl = x.Food.CoverUrl,
                Metrics = x.Food.Metrics,
                UserId = x.Food.UserId,
                Name = x.Food.Name
            })
            .GetByDataQueryAsync(q);
    }

    public async Task<DailyMenu> AddDailyMenuItem(long userId, AddDailyMenuDto dto)
    {
        if (!await dbContext.Foods.AnyAsync(x => x.Id == dto.FoodId && x.UserId == userId))
            throw new NotFoundException("Food not found");

        var date = dto.Date?.Date ?? DateTime.Now.Date;

        var menuItem = await dbContext.DailyMenus.FirstOrDefaultAsync(x => x.UserId == userId
                                                                           && x.Menu == dto.Menu
                                                                           && x.Date == date
                                                                           && x.FoodId == dto.FoodId)
                       ?? new DailyMenu();

        menuItem.UserId = userId;
        menuItem.Menu = dto.Menu;
        menuItem.Date = date;
        menuItem.FoodId = dto.FoodId;

        menuItem = dbContext.DailyMenus.Update(menuItem).Entity;
        await dbContext.SaveChangesAsync();

        return menuItem;
    }

    public async Task<int> RemoveMenuItem(long userId, long itemId)
    {
        return await dbContext.DailyMenus.Where(x => x.Id == itemId && x.UserId == userId)
            .ExecuteDeleteAsync();
    }

    #endregion

    #region Stat

    public async Task<object> Summary(long userId, DateTime? date)
    {
        var kcalNorm = await dbContext.UserNorms
                           .AsNoTracking()
                           .Select(x => new GetNormDto(x.UserId, x.Metric, x.Value))
                           .FirstOrDefaultAsync(x => x.UserId == userId && x.Metric == EnumMetrics.Kcal) ??
                       new GetNormDto(userId, EnumMetrics.Kcal, 0);

        date = date?.Date ?? DateTime.Now.Date;

        var nutrients = await dbContext.DailyMenus
            .AsNoTracking()
            .Where(x => x.UserId == userId && x.Date == date)
            .GroupBy(x => x.Menu)
            .ToDictionaryAsync(x => x.Key, x => new NutrientSummaryDto
            {
                Menu = x.Key,
                Kcal = x.Sum(dailyMenu => dailyMenu.Food.Metrics
                    .First(foodMetric => foodMetric.Metric == EnumMetrics.Kcal).Value),
                Fat = x.Sum(dailyMenu => dailyMenu.Food.Metrics
                    .First(foodMetric => foodMetric.Metric == EnumMetrics.Fat).Value),
                Protein = x.Sum(dailyMenu => dailyMenu.Food.Metrics
                    .First(foodMetric => foodMetric.Metric == EnumMetrics.Protein).Value),
                Carb = x.Sum(dailyMenu => dailyMenu.Food.Metrics
                    .First(foodMetric => foodMetric.Metric == EnumMetrics.Carb).Value)
            });

        var nutrientsNorm = await dbContext.UserNormByMenus
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .GroupBy(x => x.Menu)
            .ToDictionaryAsync(x => x.Key, x => new NutrientSummaryDto
            {
                Menu = x.Key, Kcal = x.First(foodMetric => foodMetric.Metric == EnumMetrics.Kcal).Value,
                Fat = x.First(foodMetric => foodMetric.Metric == EnumMetrics.Fat).Value,
                Protein = x.First(foodMetric => foodMetric.Metric == EnumMetrics.Protein).Value,
                Carb = x.First(foodMetric => foodMetric.Metric == EnumMetrics.Carb).Value
            });


        return new SummaryDto
        {
            KcalNorm = kcalNorm, NutrientsNorm = nutrientsNorm, Nutrients = nutrients,
            SumKcal = nutrients.Values.Sum(x => x.Kcal),
            Date = date
        };
    }

    #endregion
}