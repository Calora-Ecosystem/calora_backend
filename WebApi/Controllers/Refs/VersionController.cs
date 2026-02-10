using BRB.Core.Common.Models;
using Core.Services.Ref;
using Core.Services.Ref.Contracts;
using Microsoft.AspNetCore.Mvc;
using ResultWrapper.Library;
using Version = Core.Entities.Refs.Version;

namespace WebApi.Controllers.Refs;

[ApiController]
[Route("versions")]
public class VersionController(VersionService versionService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<WrapperGeneric<IEnumerable<Version>>>(200)]
    public async Task<Wrapper> GetAll([FromQuery] DataQueryRequest q) => await versionService.GetAll(q);

    [HttpGet("{version}")]
    [ProducesResponseType<WrapperGeneric<CheckDto>>(200)]
    public async Task<Wrapper> Check(string version) => (await versionService.Check(version), 200);

    [HttpGet("latest")]
    [ProducesResponseType<WrapperGeneric<Version>>(200)]
    public async Task<Wrapper> GetLatestVersion() => (await versionService.GetLatestVersion(), 200);

    [HttpPost]
    public async Task<Wrapper> CreateOrUpdate([FromBody] CreateOrUpdateVersionDto dto)
    {
        await versionService.CreateOrUpdate(dto);

        return 200;
    }
}