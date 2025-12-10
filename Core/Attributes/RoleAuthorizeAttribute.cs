using BRB.Core.Common.Exceptions;
using Core.Constants;
using Core.Enums;
using Core.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using ResultWrapper.Library;

namespace Core;

public class RoleAuthorizeAttribute(params EnumRole[] roles) : AuthorizeAttribute, IAuthorizationFilter
{
    private new EnumRole[] Roles { get; set; } = roles;

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var endpoint = context.ActionDescriptor.EndpointMetadata;

        if (endpoint.OfType<IAllowAnonymous>().Any())
            return;

        // Eng oxirgi qo‘yilgan attribute ni tanlaymiz (odatda Action dagisi oxirida turadi)
        var attr = endpoint.OfType<RoleAuthorizeAttribute>().LastOrDefault();
        if (attr == null)
            return;

        var user = context.HttpContext.User;
        if (user?.Identity is not { IsAuthenticated: true })
        {
            context.Result = new ObjectResult(new Wrapper(new UnauthorizedException()));
            return;
        }

        var sessionId = user.Claims.FirstOrDefault(x => x.Type == CustomClaims.SessionId)?.Value;
        var userId = user.Claims.FirstOrDefault(x => x.Type == CustomClaims.UserId)?.Value;

        if (sessionId == null || userId == null)
        {
            context.Result = new ObjectResult(new Wrapper(new SessionExpiredException()));
            return;
        }

        var cache = context.HttpContext.RequestServices.GetRequiredService<IMemoryCache>();

        if (!cache.TryGetValue($"session:{userId}:{sessionId}", out var session) || session == null)
        {
            context.Result = new ObjectResult(new Wrapper(new SessionExpiredException()));
            return;
        }


        if (attr.Roles.Any(role => user.IsInRole(role.ToString())))
        {
            return;
        }

        context.Result = new ObjectResult(new Wrapper(new ForbiddenException()));
    }
}