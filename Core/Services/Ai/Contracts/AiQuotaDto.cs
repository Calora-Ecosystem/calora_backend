namespace Core.Services.Ai.Contracts;

/// <summary>
/// Bepul AI (rasm skan + ovoz) limiti holati. Premium userda <see cref="Unlimited"/> = true.
/// </summary>
public class AiQuotaDto
{
    public bool IsPremium { get; set; }
    public bool Unlimited { get; set; }

    /// <summary>Jami bepul limit (FreeLimit + marketplace bonusi).</summary>
    public int Limit { get; set; }

    public int Used { get; set; }
    public int Remaining { get; set; }
}

public class AiQuotaConfig
{
    /// <summary>Har bir userga ro'yxatdan o'tgandan keyin beriladigan bepul AI so'rovlar soni.</summary>
    public int FreeLimit { get; set; } = 5;
}
