namespace Core.Services.Billing.Exceptions;

/// <summary>
/// A plan extra that already has subscription orders cannot be removed, since
/// <c>subscription_orders.plan_extra_id</c> uses <c>DeleteBehavior.Restrict</c>.
/// Deactivate it instead.
/// </summary>
public class PlanExtraInUseException() : Core.Exceptions.BadRequestException("plan_extra_in_use");
