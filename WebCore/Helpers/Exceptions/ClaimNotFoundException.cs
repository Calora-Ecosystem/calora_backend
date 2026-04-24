using BRB.Core.Common.Exceptions;

namespace WebCore.Helpers.Exceptions;

public class ClaimNotFoundException() : UnauthorizedException("claim_not_found");
