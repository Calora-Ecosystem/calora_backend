namespace Core.Services.Billing.Exceptions;

/// <summary>
/// Thrown when the discounted price is not lower than the original price.
/// </summary>
public class PlanExtraFeeInvalidException() : Core.Exceptions.BadRequestException("plan_extra_fee_invalid");
