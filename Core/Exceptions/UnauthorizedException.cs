using BRB.Core.Common.Exceptions.Common;

namespace Core.Exceptions;

public class UnauthorizedException : ApiException
{
    public UnauthorizedException(string message = "unauthorized") : base(message)
    {
    }

    public override int StatusCode => 401;
}
