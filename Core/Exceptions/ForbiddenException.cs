using System.Net;
using BRB.Core.Common.Exceptions.Common;

namespace Core.Exceptions;

public class ForbiddenException : ApiException
{
    public ForbiddenException(string message = "forbidden") : base(message)
    {
    }

    public override int StatusCode => (int)HttpStatusCode.Forbidden;
}
