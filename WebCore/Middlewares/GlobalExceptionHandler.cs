using System.Net;
using BRB.Core.Common.Exceptions.Common;
using ResultWrapper.Library;

namespace WebCore.Middlewares;

public class GlobalExceptionHandlerMiddleware : IMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            if (ex is ApiException apiException)
            {
                if (apiException.StatusCode == 500)
                    SentrySdk.CaptureException(apiException);
                context.Response.StatusCode = apiException.StatusCode;
                await context.Response.WriteAsJsonAsync<WrapperGeneric<object>>(
                    WrapperGeneric<object>.ResultFromException(ex, (HttpStatusCode)apiException.StatusCode));
            }
            else
            {
                SentrySdk.CaptureException(ex);
                context.Response.StatusCode = 500;
                await context.Response.WriteAsJsonAsync<WrapperGeneric<object>>(WrapperGeneric<object>
                    .ResultFromException(ex));
                Serilog.Log.Error<Exception>("Exception: {0}", ex);
            }
        }
    }
}