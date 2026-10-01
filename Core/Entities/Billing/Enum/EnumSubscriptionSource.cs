namespace Core.Entities.Billing.Enum;

/// <summary>
/// Obuna qayerdan berilgan. <see cref="Coins"/>, <see cref="Referral"/> va <see cref="Family"/>
/// muddati o'tganda recurring job tomonidan o'chiriladi; <see cref="Payment"/> obunalar
/// to'lov provayderi (RevenueCat EXPIRATION va h.k.) orqali boshqariladi.
/// </summary>
public enum EnumSubscriptionSource
{
    Payment = 1,
    Admin,
    Coins,
    Referral,

    /// <summary>Oilaviy tarif egasi bergan kod orqali (<see cref="FamilyCode"/>).</summary>
    Family
}
