namespace Core.Services.Billing.Exceptions;

public class FamilyCodeNotFoundException() : Core.Exceptions.NotFoundException("family_code_not_found");

public class FamilyCodeUsedException() : Core.Exceptions.BadRequestException("family_code_used");

public class FamilyCodeExpiredException() : Core.Exceptions.BadRequestException("family_code_expired");

public class FamilyCodeSelfException() : Core.Exceptions.BadRequestException("family_code_self");

/// <summary>
/// Oilaviy tarif faqat Click/Payme orqali: App Store / Google Play'da oilaviy mahsulot yo'q,
/// IAP orqali esa oddiy oylik narx olinib, ikkinchi odamga kod berilib yuborilardi.
/// </summary>
public class FamilyPlanStoreUnavailableException() : Core.Exceptions.BadRequestException("family_plan_store_unavailable");
