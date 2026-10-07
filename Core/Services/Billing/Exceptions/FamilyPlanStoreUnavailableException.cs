namespace Core.Services.Billing.Exceptions;

/// <summary>
/// Oilaviy tarif faqat Click/Payme orqali: App Store / Google Play'da oilaviy mahsulot yo'q.
/// </summary>
public class FamilyPlanStoreUnavailableException() : Core.Exceptions.BadRequestException("family_plan_store_unavailable");
