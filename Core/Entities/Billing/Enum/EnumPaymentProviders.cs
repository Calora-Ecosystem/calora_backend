using System.ComponentModel;

namespace Core.Entities.Billing.Enum;

public enum EnumPaymentProviders
{
    Click = 1,
    Payme,
    [Description("Revenue cat")] Iap,
}