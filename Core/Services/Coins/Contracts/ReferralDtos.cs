using System.ComponentModel.DataAnnotations;

namespace Core.Services.Coins.Contracts;

public class ReferralInfoDto
{
    /// <summary>Userning taklif kodi (masalan <c>CALORA-7K2M</c>).</summary>
    public string Code { get; set; } = null!;

    /// <summary>Kodni tasdiqlagan (ro'yxatdan o'tgan) do'stlar soni.</summary>
    public int Invited { get; set; }

    /// <summary>Ilovaga to'liq kirgan (onboarding'ni tugatgan) do'stlar soni — premium shu bo'yicha.</summary>
    public int Active { get; set; }

    /// <summary>Premium uchun kerakli do'stlar soni (default 5).</summary>
    public int FriendsGoal { get; set; }

    /// <summary>Har <see cref="FriendsGoal"/> ta do'st uchun beriladigan premium kunlari (default 30).</summary>
    public int PremiumDays { get; set; }

    /// <summary>Joriy bosqichdagi faol do'stlar (0..FriendsGoal-1).</summary>
    public int ProgressFriends { get; set; }

    /// <summary>Keyingi premiumgacha yana nechta faol do'st kerak.</summary>
    public int FriendsLeft { get; set; }

    /// <summary>0..1 oralig'ida progress.</summary>
    public double Progress { get; set; }

    /// <summary>Referral orqali olingan premiumlar soni.</summary>
    public int PremiumsEarned { get; set; }

    /// <summary>User o'zi kimningdir kodi bilan kirganmi.</summary>
    public bool IsReferred { get; set; }

    /// <summary>User taklif qilgan odamning ismi (agar kod tasdiqlangan bo'lsa).</summary>
    public string? ReferredBy { get; set; }

    /// <summary>User hali taklif kodini tasdiqlay oladimi (yangi user va hali kiritmagan).</summary>
    public bool CanApplyCode { get; set; }

    /// <summary>Taklif qilingan userga beriladigan chegirma foizi (default 10).</summary>
    public int DiscountPercent { get; set; }

    /// <summary>Joriy user chegirmadan hali foydalana oladimi.</summary>
    public bool HasDiscount { get; set; }
}

public enum EnumReferralStatus
{
    /// <summary>Kodni tasdiqlab ro'yxatdan o'tgan, lekin onboarding'ni tugatmagan.</summary>
    Joined = 1,

    /// <summary>Ilovaga to'liq kirgan — premium hisobiga qo'shilgan.</summary>
    Active
}

public class ReferredFriendDto
{
    public long UserId { get; set; }
    public string Name { get; set; } = null!;
    public string? Photo { get; set; }
    public EnumReferralStatus Status { get; set; }

    /// <summary>Kodni tasdiqlagan vaqt.</summary>
    public DateTime JoinedAt { get; set; }

    /// <summary>Ilovaga to'liq kirgan vaqt.</summary>
    public DateTime? ActivatedAt { get; set; }
}

public class ApplyReferralCodeDto
{
    [Required, MaxLength(20)] public string Code { get; set; } = null!;
}

public class ApplyReferralResultDto
{
    public string ReferrerName { get; set; } = null!;

    /// <summary>Birinchi premium xaridida chegirma foizi.</summary>
    public int DiscountPercent { get; set; }

    /// <summary>Taklif qilingan (joriy) userga berilgan coin.</summary>
    public long Reward { get; set; }
}
