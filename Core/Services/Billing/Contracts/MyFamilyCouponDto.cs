namespace Core.Services.Billing.Contracts;

/// <summary>
/// Oilaviy tarif egasiga uning yaratilgan kuponi va uni kim ishlatgani haqida ma'lumot.
/// </summary>
public class MyFamilyCouponDto
{
    public long Id { get; set; }
    public string Code { get; set; } = null!;
    public long Amount { get; set; }
    public bool IsActive { get; set; }
    public DateTime? ExpireAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UsedAt { get; set; }
    public List<long>? UsedByUserIds { get; set; }
    public List<FamilyCouponUserDto> UsedByUsers { get; set; } = new();
    public long? UsedByUserId { get; set; }
    public string? UsedByName { get; set; }
    public string Status { get; set; } = null!;
}

public class FamilyCouponUserDto
{
    public long UserId { get; set; }
    public string? Name { get; set; }
    public string? Phone { get; set; }
}
