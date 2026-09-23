namespace Core.Services.Ai.Exceptions;

/// <summary>
/// Premium bo'lmagan user bepul AI limitini tugatdi — mobile paywall'ga yo'naltiradi.
/// </summary>
public class AiFreeLimitExceededException() : Core.Exceptions.ForbiddenException("ai_free_limit_exceeded");
