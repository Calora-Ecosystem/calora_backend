namespace Core.Services.Auth.Exceptions;

public class MissingClaimException() : Core.Exceptions.UnauthorizedException("missing_claim");
