namespace Core.Services.Billing.Contracts;

public record GetCouponByIdDto
{
    public long Id { get; set; }
    public string Code { get; set; } = null!;
    public int Usages { get; set; }
    public bool OneTime { get; set; }
    public bool IsActive { get; set; }
    public long Amount { get; set; }
    public List<long>? AllowedUserIds { get; set; }
    public DateTime? ExpireAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
