using BRB.Core.Common.Models;
using Core.Entities.Crm.Enum;

namespace Core.Services.Crm.Contracts;

public record GetLeadsQuery : DataQueryRequest
{
    public EnumLeadPriority? Priority { get; set; }
    public EnumLeadStatus? Status { get; set; }
    public EnumLeadTemperature? Temperature { get; set; }
    public int? MinScore { get; set; }
    public int? MaxScore { get; set; }

    /// <summary>Filter by operator. Honoured only for HeadOfSales; operators are always scoped to themselves.</summary>
    public long? OperatorId { get; set; }

    /// <summary>When true, returns only leads not yet assigned to any operator.</summary>
    public bool? Unassigned { get; set; }

    public bool? Purchased { get; set; }

    /// <summary>
    /// Operator daily worklist: when true, returns only leads that need action today — a
    /// follow-up due today or already overdue, or a freshly-assigned lead not yet contacted
    /// (still "New"). Honoured for the operator scope.
    /// </summary>
    public bool? Agenda { get; set; }

    /// <summary>
    /// Daily activity review: when set, returns only leads the scoped operator performed some
    /// action on (contacted, moved, noted, follow-up, won/lost) on that calendar day.
    /// </summary>
    public DateTime? WorkedOn { get; set; }

    /// <summary>
    /// Distribution ordering for the Head-of-Sales / Admin leads view: most recently active leads
    /// first (newest activity on top), so the freshest leads can be handed out quickly.
    /// </summary>
    public bool? SortByActivity { get; set; }

    /// <summary>Global search across user name / email / phone.</summary>
    public string? Search { get; set; }
}
