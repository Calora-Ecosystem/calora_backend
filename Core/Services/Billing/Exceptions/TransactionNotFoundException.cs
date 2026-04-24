namespace Core.Services.Billing.Exceptions;

public class TransactionNotFoundException() : Core.Exceptions.NotFoundException("transaction_not_found");
