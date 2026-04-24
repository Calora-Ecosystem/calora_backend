using System.Net;
using BRB.Core.Common.Exceptions.Common;

namespace Core.Exceptions;

public class ExternalServiceException : ApiException
{
    public ExternalServiceException(string message = "external_service_error") : base(message)
    {
    }

    public override int StatusCode => (int)HttpStatusCode.ServiceUnavailable;
}
