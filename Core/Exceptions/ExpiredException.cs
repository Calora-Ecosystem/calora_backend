using BRB.Core.Common.Exceptions.Common;

namespace Core.Exceptions;

public class ExpiredException : ApiException
{
    public ExpiredException(string message = "expired") : base(message)
    {
    }

    public override int StatusCode => 401;
}
