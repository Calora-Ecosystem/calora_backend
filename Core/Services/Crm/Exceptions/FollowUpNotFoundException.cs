using Core.Exceptions;

namespace Core.Services.Crm.Exceptions;

public class FollowUpNotFoundException() : NotFoundException("followup_not_found");
