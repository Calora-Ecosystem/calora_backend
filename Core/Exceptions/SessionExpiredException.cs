namespace Core.Exceptions;

public class SessionExpiredException() : UnauthorizedException("session_expired");