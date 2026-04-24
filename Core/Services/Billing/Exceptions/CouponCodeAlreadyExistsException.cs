namespace Core.Services.Billing.Exceptions;

public class CouponCodeAlreadyExistsException() : Core.Exceptions.BadRequestException("coupon_code_already_exists");
