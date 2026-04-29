using System.Net;
using BRB.Core.Common.Exceptions.Common;

namespace Core.Exceptions;

public class BadRequestException : ApiException
{
    public BadRequestException(string message = "bad_request") : base(message)
    {
    }

    public override int StatusCode => (int)HttpStatusCode.BadRequest;
}
