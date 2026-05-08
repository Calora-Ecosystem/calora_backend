using System.Reflection;
using BRB.Core.Common.Exceptions.Common;
using Core;
using Microsoft.AspNetCore.Mvc;
using ResultWrapper.Library;
using WebCore;

namespace WebApi.Controllers.Refs;

[ApiController]
[Route("translations")]
public class TranslationController : ControllerBase
{
    [HttpGet("errors")]
    [ProducesResponseType<WrapperGeneric<IEnumerable<ExceptionTranslationDto>>>(200)]
    public Wrapper GetAllErrors()
    {
        var result = new List<Assembly>()
            {
                typeof(CoreConfiguration).Assembly,
                typeof(ApplicationConfigurationExtensions).Assembly
            }
            .SelectMany(a => a.GetTypes())
            .Where(t => t is { IsAbstract: false, IsClass: true }
                        && t.IsSubclassOf(typeof(ApiException))
                        && t.GetConstructor(Type.EmptyTypes) is not null)
            .Select(t =>
            {
                var instance = (ApiException)Activator.CreateInstance(t)!;
                return new ExceptionTranslationDto(t.Name, instance.Message);
            })
            .OrderBy(e => e.Type)
            .ToList();

        return (result, result.Count);
    }
}

public record ExceptionTranslationDto(string Type, string Message);
