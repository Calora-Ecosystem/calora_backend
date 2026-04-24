namespace Core.Services.Auth.Exceptions;

public class InvalidIssuerException() : Core.Exceptions.UnauthorizedException("invalid_issuer");
