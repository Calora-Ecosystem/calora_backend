using BRB.Core.Common.Exceptions;

namespace Core.Exceptions;

public class SessionExpiredException : UnauthorizedException
{
    public override int StatusCode => 401;

    public SessionExpiredException() : base("Session expired")
    {
    }
}