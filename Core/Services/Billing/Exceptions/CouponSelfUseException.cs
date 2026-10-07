namespace Core.Services.Billing.Exceptions;

public class CouponSelfUseException() : Core.Exceptions.BadRequestException("coupon_self_use");
