using System.Text.Json;
using System.Text.Json.Serialization;
using BRB.Core.Common.Exceptions;
using BRB.Core.EF.Attributes;
using Core.Enums;
using Core.Services.Ai.Contracts;
using Google.GenAI;
using Google.GenAI.Types;
using Type = Google.GenAI.Types.Type;

namespace Core.Services.Ai;

[Injectable]
public class AiService(Client client)
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

    public async Task<List<FoodResultDto>> RecognizeForFood(byte[] fileBuffer, string mimeType)
    {
        var response = await client.Models.GenerateContentAsync(
            model: "gemini-2.5-flash", contents: new Content()
            {
                Parts = new List<Part>()
                {
                    new Part()
                    {
                        Text =
                            @$"
Your are master of food world and nutriutions.
Recognize food from image or audio and return response by schema. 
Calculate metrics by {string.Join(",", Enum.GetNames<EnumMetrics>())}.
Estimate or Recognize food weight.
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
            throw new BadRequestException("Invalid result");

        return JsonSerializer.Deserialize<List<FoodResultDto>>(json, new JsonSerializerOptions()
               {
                   PropertyNameCaseInsensitive = true,
                   Converters =
                   {
                       new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
                   }
               }) ??
               throw new BadRequestException("Unable to parse result.");
    }
}