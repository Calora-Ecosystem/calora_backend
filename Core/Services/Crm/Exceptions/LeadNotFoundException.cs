using Core.Exceptions;

namespace Core.Services.Crm.Exceptions;

public class LeadNotFoundException() : NotFoundException("lead_not_found");
