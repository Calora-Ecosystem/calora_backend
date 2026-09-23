namespace Core.Entities.Billing.Enum;

/// <summary>
/// Obuna qayerdan berilgan. <see cref="Coins"/> va <see cref="Referral"/> muddati
/// o'tganda recurring job tomonidan o'chiriladi; <see cref="Payment"/> obunalar
/// to'lov provayderi (RevenueCat EXPIRATION va h.k.) orqali boshqariladi.
/// </summary>
public enum EnumSubscriptionSource
{
    Payment = 1,
    Admin,
    Coins,
    Referral
}
