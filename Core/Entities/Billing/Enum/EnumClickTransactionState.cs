namespace Core.Entities.Billing.Enum;

public enum EnumClickTransactionState
{
    Input,
    Waiting,
    PreAuth,
    Confirmed,
    Rejected,
    Refunded,
    Error,
    Cancelled
}