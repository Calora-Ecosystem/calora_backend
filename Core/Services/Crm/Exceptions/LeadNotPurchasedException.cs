using Core.Exceptions;

namespace Core.Services.Crm.Exceptions;

/// <summary>
/// Thrown when an operator tries to move a lead to Won ("Sotuv") but the user has no
/// confirmed payment yet — a sale can only be recorded against a real purchase.
/// </summary>
public class LeadNotPurchasedException()
    : BadRequestException("Foydalanuvchi hali sotib olmagan — \"Sotuv\"ga o'tkazib bo'lmaydi");
