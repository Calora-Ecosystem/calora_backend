using System.Text.Json;
using BRB.Core.EF.Attributes;
using Core.Brokers.DbContext;
using Core.Services.Ai.Exceptions;
using Core.Enums;
using Core.Services.Ai.Contracts;
using Google.GenAI;
using Google.GenAI.Types;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Type = Google.GenAI.Types.Type;

namespace Core.Services.Ai;

[Injectable]
public class AiService(Client client, AppDbContext context, IMemoryCache cache)
{
    private static readonly List<string> _foodMetrics = [
        nameof(EnumMetrics.Kcal),
        nameof(EnumMetrics.Protein),
        nameof(EnumMetrics.Fat),
        nameof(EnumMetrics.Carb),
    ];

    private GenerateContentConfig _config = new GenerateContentConfig()
    {
        ResponseMimeType = "application/json",
        ResponseSchema = new Schema()
        {
            Type = Type.ARRAY,
            Items = new Schema()
            {
                Type = Type.OBJECT,
                Properties = new Dictionary<string, Schema>()
                {
                    {
                        "name", new Schema() { Type = Type.STRING }
                    },
                    {
                        "categoryId", new Schema() { Type = Type.NUMBER }
                    },
                    {
                        "categoryName", new Schema() { Type = Type.STRING }
                    },
                    {
                        "weight", new Schema() { Type = Type.NUMBER }
                    },
                    {
                        "metrics", new Schema()
                        {
                            Type = Type.OBJECT,
                            Properties = new Dictionary<string, Schema>()
                            {
                                { nameof(EnumMetrics.Kcal),    new Schema() { Type = Type.NUMBER } },
                                { nameof(EnumMetrics.Protein), new Schema() { Type = Type.NUMBER } },
                                { nameof(EnumMetrics.Fat),     new Schema() { Type = Type.NUMBER } },
                                { nameof(EnumMetrics.Carb),    new Schema() { Type = Type.NUMBER } },
                            },
                            Required = _foodMetrics
                        }
                    }
                },
                Required = ["name", "categoryId", "weight", "metrics"]
            }
        }
    };

    private class FoodResultRaw
    {
        public string? Name { get; set; }
        public long CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public double? Weight { get; set; }
        public Dictionary<string, double>? Metrics { get; set; }
    }

    public async Task<String> GetMeta()
    {
        return await cache.GetOrCreateAsync("food_meta_for_ai", async entry =>
        {
            var categories = await context.FoodCategories
                .Select(x => new { x.Id, x.Name })
                .ToListAsync();

            // var foods = await context.Foods
            //     .Select(x => new { x.Id, x.Name })
            //     .ToListAsync();

            return JsonSerializer.Serialize(new { categories });
        }) ?? throw new InvalidMetaException();
    }

    public async Task<List<FoodResultDto>> RecognizeForFood(byte[] fileBuffer, string mimeType,
        EnumLanguage language = EnumLanguage.Uzbek)
    {
        var response = await client.Models.GenerateContentAsync(
            model: "gemini-3.6-flash", contents: new Content()
            {
                Parts = new List<Part>()
                {
                    new Part()
                    {
                        Text =
                            @$"
You are a nutrition expert. Analyze the image and identify EVERY edible or drinkable item visible —
this includes solid foods, dishes, snacks, fruits, sauces, and ALL beverages/drinks
(water, juice, soda, tea, coffee, milk, alcohol, smoothies, etc.), regardless of the container
(glass, cup, bottle, can, carton, box).
Categories: {await GetMeta()}.
Rules:
- Do not skip drinks or liquids — treat them with the same priority as solid food items.
- Always pick the closest matching categoryId (never 0 or negative).
- Estimate the weight/volume in grams of each item visible in the image (for drinks, estimate grams based on volume, e.g. 1ml ≈ 1g).
- For EVERY item you MUST provide all four nutritional metrics calculated for the estimated weight:
    Kcal   — total kilocalories (must be > 0)
    Protein — grams of protein   (must be > 0)
    Fat     — grams of fat       (must be > 0)
    Carb    — grams of carbohydrates (must be > 0)
- If exact values cannot be read from the image, use your nutritional knowledge to give a realistic estimate. Never return 0.
- Return name and categoryName in {language}.
"
                    },
                    new Part()
                    {
                        InlineData = new Blob()
                        {
                            Data = fileBuffer,
                            MimeType = mimeType
                        }
                    }
                }
            }, config: _config
        );
        
        var json = response?.Candidates?
            .FirstOrDefault()?
            .Content?
            .Parts?
            .FirstOrDefault()?
            .Text;

        if (string.IsNullOrWhiteSpace(json))
            throw new InvalidAiResultException();

        var raw = JsonSerializer.Deserialize<List<FoodResultRaw>>(json, new JsonSerializerOptions()
                  {
                      PropertyNameCaseInsensitive = true,
                  }) ??
                  throw new AiResultParseException();

        return raw.Select(r => new FoodResultDto
        {
            Name = r.Name,
            CategoryId = r.CategoryId,
            Category = r.CategoryName ?? string.Empty,
            Weight = r.Weight,
            Metrics = r.Metrics is null
                ? []
                : _foodMetrics
                    .Where(m => r.Metrics.ContainsKey(m))
                    .Select(m => new MetricResult
                    {
                        Metric = Enum.Parse<EnumMetrics>(m),
                        Value = Math.Max(r.Metrics[m], 0.1)
                    })
                    .ToList()
        }).ToList();
    }
}