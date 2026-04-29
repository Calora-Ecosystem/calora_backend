namespace Core.Services.Billing.Exceptions;

public class CouponExpireAtInvalidException() : Core.Exceptions.BadRequestException("coupon_expire_at_invalid");
