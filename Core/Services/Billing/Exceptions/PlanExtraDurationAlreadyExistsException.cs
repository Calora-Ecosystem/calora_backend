namespace Core.Services.Billing.Exceptions;

/// <summary>
/// Two active packages of the same plan cannot share a duration — the app would
/// render two identical cards.
/// </summary>
public class PlanExtraDurationAlreadyExistsException()
    : Core.Exceptions.BadRequestException("plan_extra_duration_already_exists");
