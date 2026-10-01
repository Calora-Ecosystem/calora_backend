using BRB.Core.Common.Extensions;
using Core.Services.FoodService.Exceptions;
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
using System.Linq.Expressions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using ResultWrapper.Library;

namespace Core.Services.FoodService;

[Injectable]
public class FoodService(
    AppDbContext dbContext,
    AiService aiService,
    AiQuotaService aiQuotaService,
    IHttpContextAccessor contextAccessor)
{
    private const int DefaultFoodWeightMetric = 400;
    private const int LatestFoodsLimit = 20;

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
        var category = dto.Id.HasValue
            ? await dbContext.FoodCategories.GetByIdOrThrowsNotFoundException(dto.Id.Value)
            : dbContext.FoodCategories.Add(new FoodCategory()).Entity;

        category.CoverUrl = dto.CoverUrl;
        category.Name = dto.Name;

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

    public async Task<Wrapper> GetAllFoods(long? userId, GetAllFoodsQuery q)
    {
        var queryable = dbContext.Foods.AsQueryable();

        // A user sees the catalogue plus their own foods; an anonymous caller
        // only the catalogue — never other users' private foods.
        queryable = userId is not null
            ? queryable.Where(x => x.UserId == userId || x.UserId.HasValue == false)
            : queryable.Where(x => x.UserId.HasValue == false);

        var fIds = new List<long>(); //user favourite food ids

        if (userId.HasValue)
            fIds = await dbContext.UserExtras.Where(x => x.UserId == userId.Value)
                .SelectMany(x => x.FavouriteFoods.Select(food => food.Id)).ToListAsync();

        List<long>? latestIds = null;
        if (q.Latest)
        {
            if (!userId.HasValue)
                throw new AuthorizedUserRequiredException();

            // One row per food with its most recent log. EF drops an OrderBy
            // placed before Distinct, so the old OrderBy+Distinct+Take returned
            // an arbitrary set and freshly logged foods were often missing.
            // Recency is the row Id, not DailyMenu.Date: Date is day-only and
            // can be back-dated, yet a food just logged must show up first.
            latestIds = await dbContext.DailyMenus
                .Where(x => x.UserId == userId)
                .GroupBy(x => x.FoodId)
                .Select(g => new { FoodId = g.Key, LastId = g.Max(m => m.Id) })
                .OrderByDescending(x => x.LastId)
                .Take(LatestFoodsLimit)
                .Select(x => x.FoodId)
                .ToListAsync();

            queryable = queryable.Where(x => latestIds.Contains(x.Id));
        }

        if (q.IsUserFood.HasValue && q.IsUserFood.Value)
        {
            if (!userId.HasValue)
                throw new AuthorizedUserRequiredException();
            queryable = queryable.Where(x => x.UserId == userId);
        }

        if (q.IsFavourite.HasValue && q.IsFavourite.Value)
        {
            if (!userId.HasValue)
                throw new AuthorizedUserRequiredException();
            queryable = queryable.Where(x => fIds.Contains(x.Id));
        }

        var resultQuery = queryable
            .FilterByExpressions(q.FilteringExpression);

        // Released app versions parse categoryId and coverUrl as non-null, so a
        // single uncategorised user food (manual "Create", or an AI category
        // that didn't resolve) failed the whole "Last eaten" / "My foods" page.
        // 0 means "no category" (same as food/menu); a food without its own
        // photo shows its category's picture.
        Expression<Func<Food, GetAllFoodDto>> toDto = x => new GetAllFoodDto
        {
            Id = x.Id, Name = x.Name, CategoryId = x.CategoryId ?? 0,
            CategoryName = x.Category != null ? x.Category.Name : null,
            CoverUrl = x.CoverUrl != null && x.CoverUrl != ""
                ? x.CoverUrl
                : x.Category != null ? x.Category.CoverUrl : "",
            IsFavourite = fIds.Contains(x.Id),
            Metrics = x.Metrics.Select(foodMetrics => new GetNormDto(foodMetrics.Metric, foodMetrics.Value)),
            IsUserFood = x.UserId.HasValue
        };

        if (latestIds is not null)
        {
            // "Last eaten" must be newest-first, not DB order. The set is capped
            // at LatestFoodsLimit, so order by recency in memory, then page.
            var foods = await resultQuery.Select(toDto).ToListAsync();
            var page = foods
                .OrderBy(x => latestIds.IndexOf(x.Id))
                .AsQueryable()
                .Page(q)
                .ToList();
            return (page, foods.Count);
        }

        // Without an explicit sort Skip/Take had no stable order (pages could
        // repeat or drop rows). The user's own foods come first, newest on top,
        // so a food they just created or scanned leads its category and search.
        var ordered = q.SortPropName is not null
            ? resultQuery.Sort(q)
            : userId.HasValue
                ? resultQuery
                    .OrderByDescending(x => x.UserId == userId)
                    .ThenByDescending(x => x.UserId == userId ? x.Id : 0)
                    .ThenBy(x => x.Id)
                : resultQuery.OrderBy(x => x.Id);

        return (ordered
            .Page(q)
            .Select(toDto), await resultQuery.CountAsync());
    }

    public async Task<FoodDto> GetFoodById(long foodId, long? userId)
    {
        var food = await dbContext.Foods
            .AsNoTracking()
            .Select(x => new FoodDto
            {
                // Same null-safe shape as GetAllFoods (released apps read both as non-null).
                Id = x.Id, Name = x.Name, CategoryId = x.CategoryId ?? 0,
                Description = x.Description,
                CategoryName = x.Category != null ? x.Category.Name : null,
                CoverUrl = x.CoverUrl != null && x.CoverUrl != ""
                    ? x.CoverUrl
                    : x.Category != null ? x.Category.CoverUrl : "",
                Metrics = x.Metrics.Select(foodMetrics => new GetNormDto(foodMetrics.Metric, foodMetrics.Value)),
                IsUserFood = x.UserId.HasValue,
                UserId = x.UserId,
            })
            .FirstOrDefaultAsync(x => x.Id == foodId) ?? throw new FoodNotFoundException();

        if (userId.HasValue && food.IsUserFood && food.UserId != userId)
            throw new FoodNotFoundException();

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
                Id = x.Id, Name = x.Name, CategoryId = x.CategoryId ?? 0,
                CategoryName = x.Category != null ? x.Category.Name : null,
                CoverUrl = x.CoverUrl != null && x.CoverUrl != ""
                    ? x.CoverUrl
                    : x.Category != null ? x.Category.CoverUrl : "",
                Metrics = x.Metrics.Select(foodMetrics => new GetNormDto(foodMetrics.Metric, foodMetrics.Value)),
                IsUserFood = x.UserId.HasValue
            })
            .GetByDataQueryAsync(q);
    }

    public async Task ToggleFavouriteFood(long userId, long foodId)
    {
        var food = await dbContext.Foods.GetByIdOrThrowsNotFoundException(foodId);
        var userExtra = await dbContext.UserExtras
                            .Include(userExtra => userExtra.FavouriteFoods)
                            .FirstOrDefaultAsync(x => x.UserId == userId) ??
                        throw new UserExtraNotFoundException();

        if (!userExtra.FavouriteFoods.Contains(food))
            userExtra.FavouriteFoods.Add(food);
        else userExtra.FavouriteFoods.Remove(food);

        await dbContext.SaveChangesAsync();
    }

    public async Task<Food> CreateFood(long userId, CreateFoodDto dto)
    {
        long? foodUserId = null;
        if (dto is CreateUserFood userFood)
        {
            if (userFood.UserId != userId)
                throw new FoodCreateForbiddenException();

            await dbContext.Users.ExistsOrThrowsNotFoundException(userFood.UserId);
            foodUserId = userFood.UserId;
        }
        else if (!dto.CategoryId.HasValue)
        {
            throw new FoodCategoryRequiredException();
        }

        dto.CategoryId = await ValidCategoryId(dto.CategoryId, foodUserId.HasValue);

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

    public async Task<Food> UpdateFood(long foodId, long authorizedUserId, UpdateFoodDto dto)
    {
        var food = await dbContext.Foods.GetByIdOrThrowsNotFoundException(foodId);

        if (!food.UserId.HasValue && dto.UserId.HasValue)
            throw new FoodUpdateForbiddenException();

        if (food.UserId.HasValue && !dto.UserId.HasValue)
            throw new FoodUpdateForbiddenException();

        if (food.UserId.HasValue && dto.UserId.HasValue && food.UserId != dto.UserId)
            throw new FoodUpdateForbiddenException();

        // dto.UserId comes from the client — a user's food is edited only by its owner.
        if (food.UserId.HasValue && food.UserId != authorizedUserId)
            throw new FoodUpdateForbiddenException();

        // The diary returns categoryId 0 for an uncategorised food and the app
        // sends it back on edit; that used to 404 and the edit never saved.
        dto.CategoryId = await ValidCategoryId(dto.CategoryId, food.UserId.HasValue);

        await dbContext.Transactional(async () =>
        {
            food.UserId = dto.UserId;
            food.CategoryId = dto.CategoryId;
            food.CoverUrl = dto.CoverUrl;
            food.Name = dto.Name;
            food.Description = dto.Description;

            await dbContext.SaveChangesAsync();

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
    /// A user's own food doesn't need a category, and the app can send one that
    /// doesn't exist: AI scan/voice picks the id itself (0, hallucinated,
    /// deleted) and the diary reports an uncategorised food as 0. Such an id is
    /// dropped instead of failing the whole add/edit. Catalogue foods still
    /// require a real category.
    /// </summary>
    private async Task<long?> ValidCategoryId(long? categoryId, bool isUserFood)
    {
        if (!categoryId.HasValue)
            return null;

        if (!isUserFood)
        {
            await dbContext.FoodCategories.ExistsOrThrowsNotFoundException(categoryId.Value);
            return categoryId;
        }

        return await dbContext.FoodCategories.AnyAsync(x => x.Id == categoryId.Value) ? categoryId : null;
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

    public async Task<List<FoodResultDto>> RecognizeFood(RecognizeFoodDto dto, long userId)
    {
        // Premium bo'lmaganlar uchun bepul limit (rasm va ovoz bitta hovuzdan).
        var metered = await aiQuotaService.EnsureCanUse(userId);

        var rawLanguage = contextAccessor.HttpContext?.Request.Headers.AcceptLanguage.FirstOrDefault();

        // The mobile app sends short codes ("UZ" / "ENG" / "RU"), which do not
        // match the EnumLanguage member names, so a plain Enum.TryParse always
        // failed and every request fell back to Uzbek. Map the codes explicitly
        // (still accepting the full enum names for other callers).
        var language = rawLanguage?.Trim().ToUpperInvariant() switch
        {
            "RU" or "RUSSIAN" => EnumLanguage.Russian,
            "ENG" or "EN" or "ENGLISH" => EnumLanguage.English,
            "CYRL" or "CYRILLIC" => EnumLanguage.Cyrillic,
            _ => EnumLanguage.Uzbek,
        };

        var stream = dto.File.OpenReadStream();
        byte[] buffer = new byte[dto.File.Length];
        await stream.ReadExactlyAsync(buffer, 0, buffer.Length);

        var result = await aiService.RecognizeForFood(buffer, dto.File.ContentType, language, userId);

        // Faqat muvaffaqiyatli natija limitdan yechiladi.
        if (metered && result.Count > 0)
            await aiQuotaService.Consume(userId);

        return result;
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

        var result = await query
            .Select(x => new GetMenuFoodsDto
            {
                Id = x.Id,
                Menu = x.Menu, Date = x.Date, FoodId = x.FoodId,
                FoodName = x.Food.Name,
                CategoryId = x.Food.CategoryId == null ? 0 : x.Food.CategoryId,
                CategoryName = x.Food.Category != null ? x.Food.Category.Name : "",
                CoverUrl = x.Food.CoverUrl != null && x.Food.CoverUrl != ""
                    ? x.Food.CoverUrl
                    : x.Food.Category != null ? x.Food.Category.CoverUrl : null,
                Weight = x.Weight,
                Metrics = x.Food.Metrics.Select(foodMetrics =>
                    new GetNormDto(foodMetrics.Metric, foodMetrics.Value)),
                UserId = x.Food.UserId
            })
            .GetByDataQueryAsync(q);

        result.Item1.ForEach(item =>
        {
            var diff = item.Weight / (item.Metrics.FirstOrDefault(x => x.Metric == EnumMetrics.Weight)?.Value ??
                                      DefaultFoodWeightMetric);

            item.Metrics
                .Where(x => x.Metric != EnumMetrics.Weight)
                .ForEach(x => x.Value = Math.Round(x.Value * diff, 0));
        });

        return result;
    }

    public async Task<DailyMenu> AddDailyMenuItem(long userId, AddDailyMenuDto dto)
    {
        var food = await dbContext.Foods
                       .Include(x => x.Metrics)
                       .FirstOrDefaultAsync(x =>
                           x.Id == dto.FoodId && (!x.UserId.HasValue || x.UserId == userId)) ??
                   throw new FoodNotFoundException();

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

    public async Task<DailyMenu> UpdateMenuItem(long userId, long itemId, UpdateDailyMenuDto dto)
    {
        var menuItem = await dbContext.DailyMenus
                           .FirstOrDefaultAsync(x => x.Id == itemId && x.UserId == userId)
                       ?? throw new FoodNotFoundException();

        menuItem.Weight = dto.WeightInGr;
        if (dto.Menu.HasValue) menuItem.Menu = dto.Menu.Value;
        if (dto.Date.HasValue) menuItem.Date = dto.Date.Value.Date;

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
                Kcal = x.Sum(dailyMenu => (dailyMenu.Food.Metrics
                                              .FirstOrDefault(foodMetric => foodMetric.Metric == EnumMetrics.Kcal)
                                              ?.Value ?? 0) / (dailyMenu.Food
                                              .Metrics
                                              .FirstOrDefault(foodMetric => foodMetric.Metric == EnumMetrics.Weight)
                                              ?.Value ?? DefaultFoodWeightMetric) *
                                          dailyMenu.Weight),
                Fat = x.Sum(dailyMenu => (dailyMenu.Food.Metrics
                                             .FirstOrDefault(foodMetric => foodMetric.Metric == EnumMetrics.Fat)
                                             ?.Value ?? 0) / (dailyMenu.Food
                                             .Metrics
                                             .FirstOrDefault(foodMetric => foodMetric.Metric == EnumMetrics.Weight)
                                             ?.Value ?? DefaultFoodWeightMetric) *
                                         dailyMenu.Weight),
                Protein = x.Sum(dailyMenu => (dailyMenu.Food.Metrics
                                                 .FirstOrDefault(foodMetric => foodMetric.Metric == EnumMetrics.Protein)
                                                 ?.Value ?? 0) / (dailyMenu
                                                 .Food
                                                 .Metrics
                                                 .FirstOrDefault(foodMetric => foodMetric.Metric == EnumMetrics.Weight)
                                                 ?.Value ?? DefaultFoodWeightMetric) *
                                             dailyMenu.Weight),
                Carb = x.Sum(dailyMenu => (dailyMenu.Food.Metrics
                                              .FirstOrDefault(foodMetric => foodMetric.Metric == EnumMetrics.Carb)
                                              ?.Value ?? 0) / (dailyMenu.Food
                                              .Metrics
                                              .FirstOrDefault(foodMetric => foodMetric.Metric == EnumMetrics.Weight)
                                              ?.Value ?? DefaultFoodWeightMetric) *
                                          dailyMenu.Weight),
            });


        nutrients.ForEach(x =>
        {
            // x.Value.Protein = Math.Round(x.Value.Protein * x.Value.Weight / 100, 0);
            // x.Value.Kcal = Math.Round(x.Value.Kcal * x.Value.Weight / 100, 0);
            // x.Value.Carb = Math.Round(x.Value.Carb * x.Value.Weight / 100, 0);
            // x.Value.Fat = Math.Round(x.Value.Fat * x.Value.Weight / 100, 0);
            x.Value.Protein = Math.Round(x.Value.Protein, 0);
            x.Value.Kcal = Math.Round(x.Value.Kcal, 0);
            x.Value.Carb = Math.Round(x.Value.Carb, 0);
            x.Value.Fat = Math.Round(x.Value.Fat, 0);
        });

        var nutrientsNorm = Enum.GetValues<EnumMenu>()
            .ToDictionary(x => x, menu => new NutrientSummaryDto()
            {
                Menu = menu,
                Kcal = Math.Round(menu switch
                {
                    EnumMenu.Breakfast => kcalNorm.Value * 0.25,
                    EnumMenu.Lunch => kcalNorm.Value * 0.35,
                    EnumMenu.Dinner => kcalNorm.Value * 0.30,
                    EnumMenu.Snack => kcalNorm.Value * 0.10,
                    _ => throw new InvalidMenuException()
                }, 0),
                Carb = 0,
                Fat = 0,
                Protein = 0,
                Weight = 0
            });


        return new SummaryDto
        {
            KcalNorm = kcalNorm, NutrientsNorm = nutrientsNorm, Nutrients = nutrients,
            Sum = new Dictionary<EnumMetrics, double>()
            {
                { EnumMetrics.Kcal, nutrients.Values.Sum(x => x.Kcal) },
                { EnumMetrics.Carb, nutrients.Values.Sum(x => x.Carb) },
                { EnumMetrics.Protein, nutrients.Values.Sum(x => x.Protein) },
                { EnumMetrics.Fat, nutrients.Values.Sum(x => x.Fat) },
                { EnumMetrics.Weight, nutrients.Values.Sum(x => x.Weight) },
            },
            Date = date
        };
    }

    #endregion
}