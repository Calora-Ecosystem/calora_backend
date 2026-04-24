namespace Core.Services.Billing.Rc.Exceptions;

public class RcOrderIdRequiredException() : Core.Exceptions.BadRequestException("order_id_required");
