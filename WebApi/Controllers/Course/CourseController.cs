using Core;
using Core.Enums;
using Core.Services.Course.Course;
using Core.Services.Course.Course.Contracts;
using Microsoft.AspNetCore.Mvc;
using ResultWrapper.Library;
using WebCore.Controller;

namespace WebApi.Controllers.Course;

[ApiController]
[Route("course")]
[RoleAuthorize(EnumRole.User)]
[ApiExplorerSettings(GroupName = "Course")]
public class CourseController(CourseService service) : AuthorizedController
{
    [HttpGet]
    [ProducesResponseType(typeof(WrapperGeneric<IEnumerable<GetCourseDto>>), 200)]
    public async Task<Wrapper> GetAll([FromQuery] GetCourseQueryRequest q) => await service.GetAll(q);

    [HttpPost]
    [RoleAuthorize(EnumRole.SuperAdmin)]
    public async Task<Wrapper> CreateOrUpdate(CreateOrUpdateCourseDto dto) => (await service.CreateOrUpdate(dto), 200);

    [HttpDelete("{courseId:long:min(1)}")]
    [RoleAuthorize(EnumRole.SuperAdmin)]
    public async Task<Wrapper> Remove(long courseId)
    {
        await service.Remove(courseId);
        return 200;
    }
}