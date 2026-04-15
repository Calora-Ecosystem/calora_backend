namespace Core.Services.Billing.Click.Contracts;

public enum ClickErrorType
{
    Success = 0,
    SignCheckFailed = -1,
    IncorrectParameterAmount = -2,
    ActionNotFound = -3,
    AlreadyPaid = -4,
    UserDoesNotExist = -5,
    TransactionDoesNotExist = -6,
    FailedToUpdateUser = -7,
    ErrorInRequestFromClick = -8,
    TransactionCancelled = -9,
}