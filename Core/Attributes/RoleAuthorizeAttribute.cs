using Core.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Core;

public class RoleAuthorizeAttribute(params EnumRole[] roles) : AuthorizeAttribute, IAuthorizationFilter
{
    private new EnumRole[] Roles { get; set; } = roles;

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var endpoint = context.ActionDescriptor.EndpointMetadata;

        // Eng oxirgi qo‘yilgan attribute ni tanlaymiz (odatda Action dagisi oxirida turadi)
        var attr = endpoint.OfType<RoleAuthorizeAttribute>().LastOrDefault();
        if (attr == null)
            return;

        var user = context.HttpContext.User;
        if (user?.Identity is not { IsAuthenticated: true })
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        if (attr.Roles.Any(role => user.IsInRole(role.ToString())))
        {
            return;
        }

        context.Result = new ForbidResult();
    }
}