namespace Core.Services.Auth.Exceptions;

public class TokenExpiredException() : Core.Exceptions.UnauthorizedException("token_expired");
