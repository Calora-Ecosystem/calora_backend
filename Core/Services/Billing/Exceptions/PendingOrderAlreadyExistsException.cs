namespace Core.Services.Billing.Exceptions;

public class PendingOrderAlreadyExistsException() : Core.Exceptions.BadRequestException("pending_order_already_exists");
