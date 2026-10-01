namespace Core.Services.Coins.Contracts;

/// <summary>Referral dasturi qoidalari (appsettings <c>Coins</c>) — dashboard'da ko'rsatish uchun.</summary>
public record ReferralProgramDto
{
    public int FriendsGoal { get; init; }
    public int PremiumDays { get; init; }
    public int DiscountPercent { get; init; }
    public int ReferrerReward { get; init; }
    public int ReferredReward { get; init; }
    public int ApplyWindowDays { get; init; }
}

/// <summary>
/// Referral voronkasi va asosiy ko'rsatkichlar. Kogorta — davrda kod kiritgan (taklif qilingan) do'stlar:
/// ular keyinchalik faol bo'lganmi / to'lov qilganmi (davrdan keyin bo'lsa ham) hisoblanadi.
/// </summary>
public record AdminReferralSummaryDto
{
    public DateTime From { get; init; }
    public DateTime To { get; init; }

    /// <summary>Davrda yaratilgan taklif kodlari (har ulashishda yangi kod — ulashishlar soni).</summary>
    public int CodesCreated { get; init; }

    /// <summary>Davrda kod yaratgan (ulashgan) alohida userlar.</summary>
    public int Sharers { get; init; }

    /// <summary>Davrda kod kiritgan do'stlar (taklif qilinganlar).</summary>
    public int Invited { get; init; }

    /// <summary>Ulardan ilovaga to'liq kirganlar (faol).</summary>
    public int Activated { get; init; }

    /// <summary>Hali faol bo'lmaganlar.</summary>
    public int Pending { get; init; }

    /// <summary>Ulardan kod kiritgandan keyin kamida 1 marta to'lov qilganlar.</summary>
    public int Paid { get; init; }

    /// <summary>Davrda kamida 1 do'st olib kelgan userlar.</summary>
    public int Referrers { get; init; }

    /// <summary>Bir taklif qiluvchiga o'rtacha do'st.</summary>
    public double AvgInvitesPerReferrer { get; init; }

    /// <summary>Kod → do'st konversiyasi (Invited / CodesCreated).</summary>
    public double CodeConversion { get; init; }

    public double ActivationRate { get; init; }
    public double PaidRate { get; init; }

    /// <summary>Kod kiritgandan faol bo'lishgacha o'rtacha soat.</summary>
    public double? AvgHoursToActivate { get; init; }

    /// <summary>Kod kiritgandan birinchi to'lovgacha o'rtacha kun.</summary>
    public double? AvgDaysToFirstPayment { get; init; }

    /// <summary>Davrda ro'yxatdan o'tgan yangi userlar.</summary>
    public int NewUsers { get; init; }

    /// <summary>Ulardan referral kodi bilan kelganlar.</summary>
    public int NewUsersReferred { get; init; }

    /// <summary>Yangi userlarning referral ulushi (0..1).</summary>
    public double ReferralShareOfNewUsers { get; init; }

    /// <summary>Kogortaning kod kiritgandan keyingi tasdiqlangan to'lovlari (so'm).</summary>
    public double Revenue { get; init; }

    public int Orders { get; init; }

    /// <summary>Bir to'lov qilgan do'stga o'rtacha tushum (so'm).</summary>
    public double RevenuePerPaid { get; init; }

    /// <summary>Davrda ishlatilgan referral chegirmalari.</summary>
    public int DiscountsUsed { get; init; }

    /// <summary>Davrda berilgan chegirma summasi (so'm).</summary>
    public double DiscountGiven { get; init; }

    /// <summary>Davrda taklif qiluvchilarga berilgan premiumlar (har N faol do'st uchun).</summary>
    public int PremiumGrants { get; init; }

    public int PremiumDaysGranted { get; init; }

    /// <summary>Davrda referral uchun berilgan coinlar.</summary>
    public long CoinsRewarded { get; init; }

    /// <summary>Hozir premiumga 1 ta do'st qolgan taklif qiluvchilar (marketing uchun "yaqin qolganlar").</summary>
    public int NearMilestone { get; init; }

    #region Umr bo'yi

    public int TotalInvited { get; init; }
    public int TotalActivated { get; init; }
    public int TotalReferrers { get; init; }
    public int TotalPremiumGrants { get; init; }

    #endregion

    public ReferralProgramDto Program { get; init; } = null!;

    /// <summary>Kunma-kun (davr, ko'pi bilan 366 kun).</summary>
    public List<AdminReferralDayDto> Days { get; init; } = [];
}

public record AdminReferralDayDto
{
    public DateTime Date { get; init; }
    public int Codes { get; init; }
    public int Invited { get; init; }
    public int Activated { get; init; }

    /// <summary>O'sha kuni birinchi to'lov qilgan taklif qilingan do'stlar.</summary>
    public int FirstPayments { get; init; }

    public int PremiumGrants { get; init; }
}

/// <summary>Taklif qiluvchilar reytingi qatori (davrda kod kiritgan do'stlar bo'yicha).</summary>
public record AdminReferrerRowDto
{
    /// <summary>Tanlangan saralash bo'yicha o'rni (qidiruvdan qat'i nazar).</summary>
    public int Rank { get; init; }

    public long UserId { get; init; }
    public string? Name { get; init; }
    public string? Email { get; init; }
    public string? Phone { get; init; }
    public string? Photo { get; init; }
    public DateTime? RegisteredAt { get; init; }
    public bool IsDeleted { get; init; }
    public bool IsPremium { get; init; }

    public int Invited { get; init; }
    public int Activated { get; init; }
    public int Pending { get; init; }
    public int Paid { get; init; }

    /// <summary>Taklif qilgan do'stlarning kod kiritgandan keyingi to'lovlari (so'm).</summary>
    public double Revenue { get; init; }

    /// <summary>Davrda yaratgan kodlari (ulashishlar).</summary>
    public int CodesCreated { get; init; }

    public double ActivationRate { get; init; }

    public DateTime FirstInviteAt { get; init; }
    public DateTime LastInviteAt { get; init; }

    #region Umr bo'yi

    public int TotalInvited { get; init; }
    public int TotalActivated { get; init; }
    public int PremiumGrants { get; init; }
    public int PremiumDays { get; init; }

    /// <summary>Keyingi premiumgacha yana nechta faol do'st kerak.</summary>
    public int FriendsLeft { get; init; }

    #endregion
}

/// <summary>Bitta taklif (kim → kimni) va uning natijasi.</summary>
public record AdminReferralRowDto
{
    public long Id { get; init; }
    public string? Code { get; init; }

    /// <summary>Kod kiritilgan vaqt.</summary>
    public DateTime CreatedAt { get; init; }

    public DateTime? ActivatedAt { get; init; }

    /// <summary><c>Joined</c> / <c>Active</c> / <c>Paid</c>.</summary>
    public string Status { get; init; } = null!;

    public long ReferrerId { get; init; }
    public string? ReferrerName { get; init; }
    public string? ReferrerContact { get; init; }

    public long ReferredUserId { get; init; }
    public string? ReferredName { get; init; }
    public string? ReferredContact { get; init; }
    public string? ReferredPhoto { get; init; }
    public DateTime? ReferredRegisteredAt { get; init; }
    public bool ReferredDeleted { get; init; }

    /// <summary>Kod ro'yxatdan o'tgandan keyin necha kunda kiritilgan (eski userlar ham kiritishi mumkin).</summary>
    public int? DaysAfterSignup { get; init; }

    public DateTime? FirstPaymentAt { get; init; }
    public int Orders { get; init; }

    /// <summary>Kod kiritilgandan keyingi tasdiqlangan to'lovlar (so'm).</summary>
    public double Revenue { get; init; }

    public DateTime? DiscountUsedAt { get; init; }

    /// <summary>Hozir premiummi.</summary>
    public bool IsPremium { get; init; }
}

public record AdminReferrerGrantDto
{
    public int Milestone { get; init; }
    public int Days { get; init; }
    public DateTime CreatedAt { get; init; }
}

/// <summary>Bitta taklif qiluvchining to'liq kartasi (umr bo'yi).</summary>
public record AdminReferrerDetailDto
{
    public long UserId { get; init; }
    public string? Name { get; init; }
    public string? Email { get; init; }
    public string? Phone { get; init; }
    public string? Photo { get; init; }
    public DateTime RegisteredAt { get; init; }
    public bool IsPremium { get; init; }
    public DateTime? PremiumEndsAt { get; init; }

    public int Invited { get; init; }
    public int Activated { get; init; }
    public int Pending { get; init; }
    public int Paid { get; init; }
    public double Revenue { get; init; }
    public int CodesCreated { get; init; }
    public string? LatestCode { get; init; }
    public DateTime? LastCodeAt { get; init; }
    public DateTime? FirstInviteAt { get; init; }
    public DateTime? LastInviteAt { get; init; }

    public int FriendsGoal { get; init; }
    public int ProgressFriends { get; init; }
    public int FriendsLeft { get; init; }
    public List<AdminReferrerGrantDto> Grants { get; init; } = [];

    /// <summary>Bu userni o'zi kim taklif qilgan.</summary>
    public long? ReferredById { get; init; }

    public string? ReferredByName { get; init; }
    public DateTime? ReferredAt { get; init; }

    /// <summary>Kunma-kun takliflar (oxirgi 90 kun).</summary>
    public List<AdminReferralDayDto> Days { get; init; } = [];
}
