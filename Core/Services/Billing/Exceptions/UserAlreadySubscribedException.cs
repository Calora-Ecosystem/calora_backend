namespace Core.Services.Billing.Exceptions;

public class UserAlreadySubscribedException() : Core.Exceptions.BadRequestException("user_already_subscribed");
