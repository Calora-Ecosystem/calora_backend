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
    public async Task<Wrapper> GetAll([FromQuery] GetCourseQueryRequest q) => await service.GetAll(q);

    [HttpPost]
    public async Task<Wrapper> CreateOrUpdate(CreateOrUpdateCourseDto dto)
    {
        await service.CreateOrUpdate(dto);
        return 200;
    }

    [HttpDelete("{courseId:long:min(1)}")]
    public async Task<Wrapper> Remove(long courseId)
    {
        await service.Remove(courseId);
        return 200;
    }
}