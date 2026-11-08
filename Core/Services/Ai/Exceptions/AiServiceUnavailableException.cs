namespace Core.Services.Ai.Exceptions;

/// <summary>
/// Gemini vaqtincha javob bermayapti (retry'lar tugadi, umumiy timeout yoki circuit ochiq) — 503.
/// Mobile ilova foydalanuvchiga "keyinroq urinib ko'ring" deb ko'rsatadi.
/// </summary>
public class AiServiceUnavailableException() : Core.Exceptions.ExternalServiceException("ai_service_unavailable");
