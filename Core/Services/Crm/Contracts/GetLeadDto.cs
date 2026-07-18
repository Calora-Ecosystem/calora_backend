using Core.Entities.Crm.Enum;

namespace Core.Services.Crm.Contracts;

public record GetLeadDto
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public string? UserName { get; set; }
    public string? UserEmail { get; set; }
    public string? UserPhone { get; set; }
    public bool IsRegistered { get; set; }
    public uint SubscriptionOpenedCount { get; set; }
    public bool Purchased { get; set; }
    public EnumLeadPriority Priority { get; set; }

    public long? OperatorId { get; set; }
    public string? OperatorName { get; set; }
    public EnumLeadStatus Status { get; set; }
    public int Score { get; set; }
    public EnumLeadTemperature Temperature { get; set; }
    public DateTime LastActivity { get; set; }
    public DateTime? LastContactedAt { get; set; }
    public DateTime? NextFollowUpAt { get; set; }
    public bool FollowUpOverdue { get; set; }
    public DateTime CreatedAt { get; set; }

    /// <summary>Type of the most recent operator action on this lead (null if untouched).</summary>
    public EnumLeadActivityType? LastActionType { get; set; }

    /// <summary>When the most recent operator action happened.</summary>
    public DateTime? LastActionAt { get; set; }
}
