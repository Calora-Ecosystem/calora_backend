using System.Security.Claims;
using WebCore.Helpers.Exceptions;

namespace WebCore.Helpers;

public static class HttpContextHelper
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="context"></param>
    /// <param name="claim"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    /// <exception cref="NotFoundException"></exception>
    public static T? Parse<T>(this HttpContext context, string claim)
    {
        var rawValue = context.User.FindFirstValue(claim) ??
                       throw new ClaimNotFoundException();
        var value = Convert.ChangeType(rawValue, typeof(T?));
        return (T?)value;
    }

    public static T ParseRequired<T>(this HttpContext context, string claim) => context.Parse<T>(claim) ??
        throw new ClaimNotFoundException();
}