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

    /// <summary>Global search across user name / email / phone.</summary>
    public string? Search { get; set; }
}
