using Core.Exceptions;

namespace Core.Services.Crm.Exceptions;

public class LostReasonRequiredException() : BadRequestException("lost_reason_required");
