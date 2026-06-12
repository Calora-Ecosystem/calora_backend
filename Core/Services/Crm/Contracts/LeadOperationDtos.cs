using System.ComponentModel.DataAnnotations;
using Core.Entities.Crm.Enum;

namespace Core.Services.Crm.Contracts;

public record MoveStatusDto
{
    [Required] public EnumLeadStatus Status { get; set; }

    /// <summary>Reason, required when moving to Lost.</summary>
    [MaxLength(300)] public string? Reason { get; set; }
}

public record GetLeadActivityDto
{
    public long Id { get; set; }
    public EnumLeadActivityType Type { get; set; }
    public string Description { get; set; } = null!;
    public string? ActorName { get; set; }
    public DateTime CreatedAt { get; set; }
}

public record CreateFollowUpDto
{
    [Required] public DateTime DueAt { get; set; }
    [MaxLength(500)] public string? Note { get; set; }
}

public record GetFollowUpDto
{
    public long Id { get; set; }
    public long LeadId { get; set; }
    public string? LeadName { get; set; }
    public string? LeadPhone { get; set; }
    public DateTime DueAt { get; set; }
    public string? Note { get; set; }
    public bool IsDone { get; set; }
    public bool Overdue { get; set; }
}

public enum EnumStatsPeriod
{
    Day = 1,
    Week,
    Month
}
