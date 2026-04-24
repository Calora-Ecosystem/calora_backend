namespace Core.Services.Billing.Exceptions;

public class CouponAlreadyUsedException() : Core.Exceptions.BadRequestException("coupon_already_used");
