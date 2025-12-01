using Core.Services.Ai.Contracts;
using Mscc.GenerativeAI;

namespace Core.Services.Ai;

public interface IAiService
{
    public Task<FoodResultDto> ScanFood(List<InlineData> data);
    public Task<FoodResultDto> RecognizeFood(List<InlineData> data);
}