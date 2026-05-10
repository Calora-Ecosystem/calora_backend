using BRB.Core.Common.Models;
using Core.Entities.Crm.Enum;

namespace Core.Services.Crm.Contracts;

public record GetLeadsQuery : DataQueryRequest
{
    public EnumLeadPriority? Priority { get; set; }
}
