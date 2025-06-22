using System.Reflection;
using BRB.Core.Common.Helpers;
using BRB.Core.Web.Attributes;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace WebCore.Filters.Swagger;

public class PermissionFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var authorizeAttribute = context.MethodInfo.GetCustomAttribute<AuthorizeAttribute>() ?? context.MethodInfo.DeclaringType?.GetCustomAttribute<AuthorizeAttribute>();

        if (authorizeAttribute is { })
        {
            operation.Summary += $"REQUIRED PERMISSION CODE(s): {(authorizeAttribute.Arguments != null && authorizeAttribute.Arguments.Length != 0 ? SerializerHelper.ToJsonString(authorizeAttribute.Arguments![0]) : "[NONE]")}";
        }
    }
}