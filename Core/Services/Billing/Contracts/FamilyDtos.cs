using System.ComponentModel.DataAnnotations;

namespace Core.Services.Billing.Contracts;

/// <summary>Oilaviy tarif egasining ikkinchi odam uchun kodi.</summary>
public class FamilyCodeDto
{
    public string Code { get; set; } = null!;

    /// <summary>Kod beradigan premium muddati (oy).</summary>
    public int Months { get; set; }

    public EnumFamilyCodeStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }

    /// <summary>Kod shu vaqtgacha faollashtirilishi kerak.</summary>
    public DateTime ExpireAt { get; set; }

    public DateTime? RedeemedAt { get; set; }

    /// <summary>Kodni faollashtirgan userning ismi.</summary>
    public string? RedeemedBy { get; set; }
}

public enum EnumFamilyCodeStatus
{
    /// <summary>Hali ishlatilmagan, faollashtirish mumkin.</summary>
    Active = 1,
    Redeemed,
    Expired
}

public class RedeemFamilyCodeDto
{
    [Required, MaxLength(20)] public string Code { get; set; } = null!;
}

public class RedeemFamilyCodeResultDto
{
    /// <summary>Kodni bergan (oilaviy tarif egasi) userning ismi.</summary>
    public string? OwnerName { get; set; }

    public int Months { get; set; }
    public DateTime EndsAt { get; set; }

    /// <summary>Premium JWT ichida — ilova tokenni darhol yangilashi kerak.</summary>
    public bool RequiresTokenRefresh { get; set; } = true;
}
