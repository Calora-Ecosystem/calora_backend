namespace Core.Services.Auth.Exceptions;

public class InvalidTokenException() : Core.Exceptions.UnauthorizedException("invalid_token");
