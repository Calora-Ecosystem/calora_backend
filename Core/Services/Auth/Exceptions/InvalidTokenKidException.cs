namespace Core.Services.Auth.Exceptions;

public class InvalidTokenKidException() : Core.Exceptions.UnauthorizedException("invalid_token_kid");
