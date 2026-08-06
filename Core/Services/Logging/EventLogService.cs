using BRB.Core.EF.Attributes;
using Core.Brokers.DbContext;
using Core.Entities.Logging;
using Core.Enums;
using Microsoft.Extensions.Logging;

namespace Core.Services.Logging;

/// <summary>
/// Umumiy hodisa jurnaliga yozish uchun yagona kirish nuqtasi — istalgan subsystem
/// (AI, to'lov, notification va h.k.) shu orqali o'z hodisalarini qayd etadi.
/// Yozish muvaffaqiyatsiz bo'lsa, chaqiruvchining asosiy oqimini buzmaslik uchun
/// xato yutiladi (faqat ogohlantirish sifatida log qilinadi).
/// </summary>
[Injectable]
public class EventLogService(AppDbContext context, ILogger<EventLogService> logger)
{
    private const int MaxErrorMessageLength = 2000;

    public async Task LogAsync(
        string source,
        string outcome,
        EnumEventStatus status,
        long? userId = null,
        long? durationMs = null,
        string? errorType = null,
        string? errorMessage = null,
        Dictionary<string, string>? metadata = null)
    {
        try
        {
            context.EventLogs.Add(new EventLog
            {
                CreatedAt = DateTime.Now,
                Source = source,
                Outcome = outcome,
                Status = status,
                UserId = userId,
                DurationMs = durationMs,
                ErrorType = errorType,
                ErrorMessage = Truncate(errorMessage, MaxErrorMessageLength),
                Metadata = metadata,
            });

            await context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to persist event log (source={Source}, outcome={Outcome})",
                source, outcome);
        }
    }

    private static string? Truncate(string? value, int maxLength) =>
        value is null || value.Length <= maxLength ? value : value[..maxLength] + "...(truncated)";
}
