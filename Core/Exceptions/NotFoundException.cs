using System.Net;
using BRB.Core.Common.Exceptions.Common;

namespace Core.Exceptions;

public class NotFoundException : ApiException
{
    public NotFoundException(string message = "not_found") : base(message)
    {
    }

    public override int StatusCode => (int)HttpStatusCode.NotFound;
}
