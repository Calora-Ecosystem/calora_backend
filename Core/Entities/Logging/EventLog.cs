using System.ComponentModel.DataAnnotations.Schema;
using BRB.Core.Common.Models.Base;
using Core.Enums;
using Microsoft.EntityFrameworkCore;

namespace Core.Entities.Logging;

/// <summary>
/// Umumiy (general-purpose) hodisa jurnali — istalgan subsystem (AI, to'lov, notification
/// va h.k.) o'z hodisalarini shu bitta jadvalga yozadi. Analitika (nechta hodisa, qanday
/// holatda tugagan, xato bo'lsa sababi) shu jadval ustida quriladi.
/// </summary>
[Index(nameof(CreatedAt))]
[Index(nameof(Source))]
[Index(nameof(Status))]
[Index(nameof(UserId))]
public class EventLog : ModelBase<long>
{
    public DateTime CreatedAt { get; set; }

    /// <summary>Hodisa qaysi subsystemdan kelganini bildiruvchi nuqta bilan ajratilgan kod, masalan "ai.food_recognition".</summary>
    public string Source { get; set; } = null!;

    /// <summary>Shu Source ichidagi aniq natija kodi, masalan "success", "empty_result", "provider_error".</summary>
    public string Outcome { get; set; } = null!;

    public EnumEventStatus Status { get; set; }

    public long? UserId { get; set; }
    public long? DurationMs { get; set; }

    /// <summary>Exception turi (Status=Error bo'lsa) — masalan "AiResultParseException".</summary>
    public string? ErrorType { get; set; }

    /// <summary>Qisqartirilgan xato xabari — diagnostika uchun.</summary>
    public string? ErrorMessage { get; set; }

    /// <summary>Source'ga xos qo'shimcha ma'lumot (model, itemCount, tokenlar va h.k.) — schema o'zgarishisiz kengaytiriladi.</summary>
    [Column(TypeName = "jsonb")]
    public Dictionary<string, string>? Metadata { get; set; }
}
