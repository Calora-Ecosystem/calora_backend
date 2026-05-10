using Core.Services.Crm.Enum;

namespace Core.Services.Crm.Contracts;

public record HandleLeadEventDto(long UserId, EnumLeadEvent Event);
