using System.Text.Json;
using BRB.Core.Common.Extensions;
using Core.Services.Ai.Contracts;
using Mscc.GenerativeAI;

namespace Core.Services.Ai;

public class GeminiService(GenerativeModel model) : IAiService
{
    public async Task<FoodResultDto> ScanFood(List<InlineData> data)
    {
        var request = new GenerateContentRequest();
        foreach (var inlineData in data)
        {
            request.AddPart(inlineData);
        }

        var response = await model.GenerateContent(request);

        if (response.Text.IsNullOrEmpty())
            throw new Exception("Generation failed.");

        response.CheckResponse();

        return JsonSerializer.Deserialize<FoodResultDto>(response.Text!)!;
    }

    public Task<FoodResultDto> RecognizeFood()
    {
        throw new NotImplementedException();
    }
}