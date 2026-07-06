using System.Text.Json;
using System.Text.Json.Serialization;
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
                        "name", new Schema()
                        {
                            Type = Type.STRING,
                        }
                    },
                    {
                        "categoryId", new Schema()
                        {
                            Type = Type.NUMBER
                        }
                    },
                    {
                        "categoryName", new Schema()
                        {
                            Type = Type.STRING,
                        }
                    },
                    {
                        "weight", new Schema()
                        {
                            Type = Type.NUMBER,
                        }
                    },
                    {
                        "metrics", new Schema()
                        {
                            Type = Type.ARRAY,
                            Items = new Schema()
                            {
                                Type = Type.OBJECT,
                                Properties = new Dictionary<string, Schema>()
                                {
                                    {
                                        "metric", new Schema()
                                        {
                                            Type = Type.STRING,
                                            Enum = Enum.GetNames<EnumMetrics>().ToList()
                                        }
                                    },
                                    {
                                        "value", new Schema()
                                        {
                                            Type = Type.NUMBER
                                        }
                                    }
                                },
                                Required = ["metric", "value"]
                            }
                        }
                    }
                },
                Required = ["metrics"]
            }
        }
    };

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
            model: "gemini-3-flash-preview", contents: new Content()
            {
                Parts = new List<Part>()
                {
                    new Part()
                    {
                        Text =
                            @$"
Your are master of food world and nutritions.
Recognize food from image or audio and return response by schema.
Categories: {await GetMeta()}.
Always return the closest matching category ID. Never return a value less than or equal to 0.
Calculate metrics by {string.Join(",", Enum.GetNames<EnumMetrics>())}.
Estimate or Recognize food weight.
Return all results in {language.ToString()}.
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

        return JsonSerializer.Deserialize<List<FoodResultDto>>(json, new JsonSerializerOptions()
               {
                   PropertyNameCaseInsensitive = true,
                   Converters =
                   {
                       new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
                   }
               }) ??
               throw new AiResultParseException();
    }
}