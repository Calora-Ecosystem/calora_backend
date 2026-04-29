namespace Core.Services.Billing.Exceptions;

public class CouponUsersNotFoundException() : Core.Exceptions.BadRequestException("coupon_users_not_found");
