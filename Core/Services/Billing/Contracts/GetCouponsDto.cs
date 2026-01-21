namespace Core.Services.Billing.Contracts;

public record GetCouponsDto
{
    public long Id { get; set; }
    public int Usages { get; set; }
    public string Code { get; set; } = null!;
    public DateTime? ExpireAt { get; set; }
    public List<long>? AllowedUserIds { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool OneTime { get; set; }
}