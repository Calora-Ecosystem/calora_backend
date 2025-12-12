using BRB.Core.Common.Exceptions;
using BRB.Core.Common.Extensions;
using BRB.Core.Common.Models;
using BRB.Core.EF.Attributes;
using BRB.Core.EF.Extensions;
using Core.Brokers.DbContext;
using Core.Entities.FoodEntites;
using Core.Enums;
using Core.Services.Ai;
using Core.Services.Ai.Contracts;
using Core.Services.FoodService.Contracts.Category;
using Core.Services.FoodService.Contracts.FoodDtos;
using Core.Services.User.Contracts;
using Microsoft.EntityFrameworkCore;
using ResultWrapper.Library;

namespace Core.Services.FoodService;

[Injectable]
public class FoodService(AppDbContext dbContext, AiService aiService)
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

        var metricsDict =
            new List<EnumMetrics>([
                EnumMetrics.Weight, EnumMetrics.Kcal, EnumMetrics.Carb, EnumMetrics.Fat, EnumMetrics.Protein
            ]).ToDictionary(x => x, x => new GetNormDto(userId ?? 0, x, 0));

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

    public async Task<Food> CreateFood(long userId, CreateFoodDto dto)
    {
        long? foodUserId = null;
        if (dto is CreateUserFood userFood)
        {
            if (userFood.UserId != userId)
                throw new BadRequestException("Deny to create food for another user");

            await dbContext.Users.ExistsOrThrowsNotFoundException(userFood.UserId);
            foodUserId = userFood.UserId;
        }

        await dbContext.FoodCategories.ExistsOrThrowsNotFoundException(dto.CategoryId);

        var food = dbContext.Foods.Add(new Food()
        {
            UserId = foodUserId,
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

        if (!food.UserId.HasValue && dto.UserId.HasValue)
            throw new BadRequestException("Unable to update this food");

        if (food.UserId.HasValue && !dto.UserId.HasValue)
            throw new BadRequestException("Unable to update this food");

        if (food.UserId.HasValue && dto.UserId.HasValue && food.UserId != dto.UserId)
            throw new BadRequestException("Unable to update this food");

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

    public async Task<List<FoodResultDto>> RecognizeFood(RecognizeFoodDto dto)
    {
        var stream = dto.File.OpenReadStream();
        byte[] buffer = new byte[dto.File.Length];
        await stream.ReadExactlyAsync(buffer, 0, buffer.Length);

        return await aiService.RecognizeForFood(buffer, dto.File.ContentType);
    }

    #endregion

    #region Menu

    public async Task<Wrapper> GetMenuFoods(long userId, EnumMenu? menu, DateTime? date, DataQueryRequest q)
    {
        date ??= DateTime.Now.Date;
        var query = dbContext.DailyMenus
            .AsNoTracking()
            .AsSplitQuery()
            .Where(x => x.UserId == userId && x.Date == date.Value.Date);

        if (menu.HasValue) query = query.Where(x => x.Menu == menu.Value);

        return await query
            .Select(x => new GetMenuFoodsDto
            {
                Menu = x.Menu, Date = x.Date, FoodId = x.FoodId,
                FoodName = x.Food.Name,
                CategoryId = x.Food.CategoryId,
                CategoryName = x.Food.Category.Name,
                CoverUrl = x.Food.CoverUrl,
                Metrics = x.Food.Metrics.Select(foodMetrics => new GetNormDto(foodMetrics.Metric, foodMetrics.Value)),
                UserId = x.Food.UserId
            })
            .GetByDataQueryAsync(q);
    }

    public async Task<DailyMenu> AddDailyMenuItem(long userId, AddDailyMenuDto dto)
    {
        if (!await dbContext.Foods.AnyAsync(x => x.Id == dto.FoodId && (!x.UserId.HasValue || x.UserId == userId)))
            throw new NotFoundException("Food not found");

        var date = dto.Date?.Date ?? DateTime.Now.Date;

        var menuItem = await dbContext.DailyMenus.FirstOrDefaultAsync(x => x.UserId == userId
                                                                           && x.Menu == dto.Menu
                                                                           && x.Date == date
                                                                           && x.FoodId == dto.FoodId)
                       ?? new DailyMenu()
                       {
                           UserId = userId
                       };

        menuItem.Menu = dto.Menu;
        menuItem.Date = date;
        menuItem.FoodId = dto.FoodId;
        menuItem.Weight = dto.WeightInGr;

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
                           .Where(x => x.UserId == userId && x.Metric == EnumMetrics.Kcal)
                           .Select(x => new GetNormDto(x.UserId, x.Metric, x.Value))
                           .FirstOrDefaultAsync() ??
                       new GetNormDto(userId, EnumMetrics.Kcal, 0);

        date = date?.Date ?? DateTime.Now.Date;

        var nutrients = await dbContext.DailyMenus
            .AsNoTracking()
            .Where(x => x.UserId == userId && x.Date == date)
            .Include(x => x.Food)
            .ThenInclude(x => x.Metrics)
            .GroupBy(x => x.Menu)
            .ToDictionaryAsync(x => x.Key, x => new NutrientSummaryDto
            {
                Menu = x.Key,
                Weight = x.Sum(dailyMenu => dailyMenu.Weight),
                Kcal = x.Sum(dailyMenu => dailyMenu.Food.Metrics
                    .FirstOrDefault(foodMetric => foodMetric.Metric == EnumMetrics.Kcal)?.Value ?? 0),
                Fat = x.Sum(dailyMenu => dailyMenu.Food.Metrics
                    .FirstOrDefault(foodMetric => foodMetric.Metric == EnumMetrics.Fat)?.Value ?? 0),
                Protein = x.Sum(dailyMenu => dailyMenu.Food.Metrics
                    .FirstOrDefault(foodMetric => foodMetric.Metric == EnumMetrics.Protein)?.Value ?? 0),
                Carb = x.Sum(dailyMenu => dailyMenu.Food.Metrics
                    .FirstOrDefault(foodMetric => foodMetric.Metric == EnumMetrics.Carb)?.Value ?? 0),
            });


        nutrients.ForEach(x =>
        {
            x.Value.Protein = x.Value.Protein * x.Value.Weight / 100;
            x.Value.Kcal = x.Value.Kcal * x.Value.Weight / 100;
            x.Value.Carb = x.Value.Carb * x.Value.Weight / 100;
            x.Value.Fat = x.Value.Fat * x.Value.Weight / 100;
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
                Carb = x.First(foodMetric => foodMetric.Metric == EnumMetrics.Carb).Value,
                Weight = x.First(foodMetric => foodMetric.Metric == EnumMetrics.Weight).Value
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