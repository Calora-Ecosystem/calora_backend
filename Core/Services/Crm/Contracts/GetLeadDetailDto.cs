using Core.Entities.Billing.Enum;
using Core.Entities.Crm.Enum;
using Core.Enums;

namespace Core.Services.Crm.Contracts;

public record GetLeadDetailDto
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
    public DateTime CreatedAt { get; set; }

    // Payment info captured on a won deal.
    public EnumPaymentProviders? PaymentProvider { get; set; }
    public long? WonAmount { get; set; }
    public DateTime? WonAt { get; set; }

    // Profile snapshot from UserExtra.
    public int? Age { get; set; }
    public EnumGender? Gender { get; set; }
    public double? Weight { get; set; }
    public double? Height { get; set; }
    public EnumPurpose? Purpose { get; set; }
}

public record GetNoteDto
{
    public long Id { get; set; }
    public string Text { get; set; } = null!;
    public string OperatorName { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}
