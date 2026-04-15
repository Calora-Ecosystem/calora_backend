namespace Core.Entities.Billing.Payme;

public enum EnumPaymeTransactionStatus
{
    PaidCancelled = -2,
    PendingCancelled = -1,
    Pending = 1,
    Paid = 2,
}