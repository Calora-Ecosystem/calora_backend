using System.Net;
using Core.Constants;
using Core.Enums;
using Core.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ResultWrapper.Library;

namespace Core.Attributes;

public class RoleAuthorizeAttribute(params EnumRole[] roles) : AuthorizeAttribute, IAuthorizationFilter
{
    private new EnumRole[] Roles { get; set; } = roles;
    public EnumSPlans[]? Plans { get; set; }

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
        if (user.Identity is not { IsAuthenticated: true })
        {
            context.HttpContext.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
            context.Result = new ObjectResult(new Wrapper(new UnauthorizedException()));
            return;
        }

        var environment = context.HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>();

        //Disable session check in development
        if (!environment.IsDevelopment())
        {
            var sessionId = user.Claims.FirstOrDefault(x => x.Type == CustomClaims.SessionId)?.Value;
            var userId = user.Claims.FirstOrDefault(x => x.Type == CustomClaims.UserId)?.Value;

            if (sessionId == null || userId == null)
            {
                context.HttpContext.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                context.Result =
                    new ObjectResult(new Wrapper(new SessionExpiredException(), HttpStatusCode.Unauthorized));
                return;
            }

            var cache = context.HttpContext.RequestServices.GetRequiredService<IMemoryCache>();

            if (!cache.TryGetValue($"session:{userId}:{sessionId}", out var session) || session == null)
            {
                context.HttpContext.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                context.Result =
                    new ObjectResult(new Wrapper(new SessionExpiredException(), HttpStatusCode.Unauthorized));
                return;
            }
        }

        if (attr.Roles.Any(role => user.IsInRole(role.ToString())))
        {
            if (attr.Plans == null)
                return;
        }


        if (attr.Plans != null)
        {
            if (!attr.Plans.Any())
                return;

            var plan = user.Claims.FirstOrDefault(x => x.Type == CustomClaims.Plan)?.Value;

            if (plan != null && Enum.IsDefined(typeof(EnumSPlans), plan) &&
                attr.Plans!.Contains(Enum.Parse<EnumSPlans>(plan)))
                return;

            context.HttpContext.Response.StatusCode = (int)HttpStatusCode.Forbidden;
            context.Result =
                new ObjectResult(new Wrapper(new ForbiddenException("You don't have access to this resource")));
        }

        context.HttpContext.Response.StatusCode = (int)HttpStatusCode.Forbidden;
        context.Result = new ObjectResult(new Wrapper(new ForbiddenException(), HttpStatusCode.Forbidden));
    }
}