using System.Diagnostics;
using System.Text.Json;
using BRB.Core.EF.Attributes;
using Core.Brokers.DbContext;
using Core.Services.Ai.Exceptions;
using Core.Enums;
using Core.Services.Ai.Contracts;
using Core.Services.Logging;
using Google.GenAI;
using Google.GenAI.Types;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Type = Google.GenAI.Types.Type;

namespace Core.Services.Ai;

[Injectable]
public class AiService(
    Client client, AppDbContext context, IMemoryCache cache, ILogger<AiService> logger, EventLogService eventLog)
{
    private const string Model = "gemini-2.5-flash";
    private const string EventSource = "ai.food_recognition";
    private static readonly List<string> _foodMetrics = [
        nameof(EnumMetrics.Kcal),
        nameof(EnumMetrics.Protein),
        nameof(EnumMetrics.Fat),
        nameof(EnumMetrics.Carb),
    ];

    private GenerateContentConfig _config = new GenerateContentConfig()
    {
        ResponseMimeType = "application/json",
        ThinkingConfig = new ThinkingConfig()
        {
            ThinkingBudget = 0
        },
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
        EnumLanguage language = EnumLanguage.Uzbek, long? userId = null)
    {
        SentrySdk.SetTag("ai_model", Model);
        SentrySdk.SetTag("ai_mime_type", mimeType);
        SentrySdk.SetTag("ai_file_size_bytes", fileBuffer.Length.ToString());
        SentrySdk.SetTag("ai_language", language.ToString());

        var stopwatch = Stopwatch.StartNew();
        GenerateContentResponse? response;
        try
        {
            response = await client.Models.GenerateContentAsync(
                model: Model, contents: new Content()
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
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            SentrySdk.SetTag("ai_duration_ms", stopwatch.ElapsedMilliseconds.ToString());
            logger.LogError(ex,
                "Gemini request failed after {DurationMs}ms (model={Model}, mimeType={MimeType}, fileSizeBytes={FileSizeBytes})",
                stopwatch.ElapsedMilliseconds, Model, mimeType, fileBuffer.Length);
            await eventLog.LogAsync(EventSource, "provider_error", EnumEventStatus.Error, userId,
                stopwatch.ElapsedMilliseconds, ex.GetType().Name, ex.Message,
                new Dictionary<string, string> { ["model"] = Model, ["mimeType"] = mimeType });
            throw;
        }

        stopwatch.Stop();

        var candidate = response?.Candidates?.FirstOrDefault();
        var finishReason = candidate?.FinishReason;
        var blockReason = response?.PromptFeedback?.BlockReason;
        var usage = response?.UsageMetadata;

        SentrySdk.SetTag("ai_duration_ms", stopwatch.ElapsedMilliseconds.ToString());
        SentrySdk.SetTag("ai_finish_reason", finishReason?.ToString() ?? "none");

        if (blockReason is not null)
            logger.LogWarning(
                "Gemini blocked the prompt: {BlockReason} {BlockReasonMessage} (model={Model}, durationMs={DurationMs})",
                blockReason, response?.PromptFeedback?.BlockReasonMessage, Model, stopwatch.ElapsedMilliseconds);

        if (finishReason is not null && finishReason != FinishReason.STOP)
            logger.LogWarning(
                "Gemini candidate finished with {FinishReason}: {FinishMessage} (model={Model}, durationMs={DurationMs})",
                finishReason, candidate?.FinishMessage, Model, stopwatch.ElapsedMilliseconds);

        var json = candidate?.Content?.Parts?.FirstOrDefault()?.Text;

        if (string.IsNullOrWhiteSpace(json))
        {
            var invalidResultEx = new InvalidAiResultException();
            logger.LogError(invalidResultEx,
                "Gemini returned no text (model={Model}, durationMs={DurationMs}, finishReason={FinishReason}, blockReason={BlockReason})",
                Model, stopwatch.ElapsedMilliseconds, finishReason, blockReason);
            SentrySdk.CaptureException(invalidResultEx);
            await eventLog.LogAsync(EventSource, "invalid_result", EnumEventStatus.Error, userId,
                stopwatch.ElapsedMilliseconds, nameof(InvalidAiResultException), null,
                new Dictionary<string, string>
                {
                    ["model"] = Model,
                    ["finishReason"] = finishReason?.ToString() ?? "",
                    ["blockReason"] = blockReason?.ToString() ?? "",
                });
            throw invalidResultEx;
        }

        List<FoodResultRaw> raw;
        try
        {
            raw = JsonSerializer.Deserialize<List<FoodResultRaw>>(json, new JsonSerializerOptions()
            {
                PropertyNameCaseInsensitive = true,
            }) ?? throw new AiResultParseException();
        }
        catch (JsonException jsonEx)
        {
            var parseEx = new AiResultParseException();
            SentrySdk.ConfigureScope(scope => scope.SetExtra("ai_raw_response", Truncate(json, 2000)));
            logger.LogError(jsonEx,
                "Failed to parse Gemini JSON (model={Model}, durationMs={DurationMs}): {RawJson}",
                Model, stopwatch.ElapsedMilliseconds, Truncate(json, 2000));
            SentrySdk.CaptureException(parseEx);
            await eventLog.LogAsync(EventSource, "parse_error", EnumEventStatus.Error, userId,
                stopwatch.ElapsedMilliseconds, jsonEx.GetType().Name, jsonEx.Message,
                new Dictionary<string, string> { ["model"] = Model });
            throw parseEx;
        }

        if (raw.Count == 0)
            logger.LogWarning(
                "Gemini recognized no items in the image (model={Model}, durationMs={DurationMs}, promptTokens={PromptTokens}, imageTokens={ImageTokens})",
                Model, stopwatch.ElapsedMilliseconds,
                usage?.PromptTokenCount,
                usage?.PromptTokensDetails?.FirstOrDefault(d => d.Modality == MediaModality.IMAGE)?.TokenCount);

        foreach (var item in raw)
        {
            if (item.CategoryId <= 0)
                logger.LogWarning("Gemini returned an invalid categoryId {CategoryId} for item {ItemName}",
                    item.CategoryId, item.Name);

            if (item.Metrics is null || _foodMetrics.Any(m => !item.Metrics.ContainsKey(m)))
                logger.LogWarning("Gemini returned incomplete metrics for item {ItemName}: {Metrics}",
                    item.Name, item.Metrics is null ? "null" : string.Join(",", item.Metrics.Keys));
            else
                foreach (var m in _foodMetrics.Where(m => item.Metrics[m] <= 0))
                    logger.LogWarning("Gemini returned non-positive {Metric}={Value} for item {ItemName}",
                        m, item.Metrics[m], item.Name);
        }

        logger.LogInformation(
            "Gemini recognized {ItemCount} item(s) (model={Model}, durationMs={DurationMs}, promptTokens={PromptTokens}, candidateTokens={CandidateTokens})",
            raw.Count, Model, stopwatch.ElapsedMilliseconds, usage?.PromptTokenCount, usage?.CandidatesTokenCount);

        await eventLog.LogAsync(
            EventSource,
            raw.Count == 0 ? "empty_result" : "success",
            raw.Count == 0 ? EnumEventStatus.Warning : EnumEventStatus.Success,
            userId,
            stopwatch.ElapsedMilliseconds,
            metadata: new Dictionary<string, string>
            {
                ["model"] = Model,
                ["mimeType"] = mimeType,
                ["itemCount"] = raw.Count.ToString(),
                ["promptTokens"] = usage?.PromptTokenCount?.ToString() ?? "",
                ["candidateTokens"] = usage?.CandidatesTokenCount?.ToString() ?? "",
                ["finishReason"] = finishReason?.ToString() ?? "",
            });

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

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength] + "...(truncated)";
}