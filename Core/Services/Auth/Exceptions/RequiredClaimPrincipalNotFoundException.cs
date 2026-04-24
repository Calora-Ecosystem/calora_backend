namespace Core.Services.Auth.Exceptions;

public class RequiredClaimPrincipalNotFoundException() : Core.Exceptions.UnauthorizedException("required_claim_principal_not_found");
