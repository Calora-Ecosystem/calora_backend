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
    public DateTime LastActivity { get; set; }
}
