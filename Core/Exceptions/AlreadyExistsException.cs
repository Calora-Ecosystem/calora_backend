using System.Net;
using BRB.Core.Common.Exceptions.Common;

namespace Core.Exceptions;

public class AlreadyExistsException : ApiException
{
    public AlreadyExistsException(string message = "already_exists") : base(message)
    {
    }

    public override int StatusCode => (int)HttpStatusCode.Conflict;
}
