using BRB.Core.Common.Models;
using Core;
using Core.Entities.Course.Enum;
using Core.Enums;
using Core.Services.Course.Course;
using Core.Services.Course.Lesson;
using Core.Services.Course.Lesson.Contracts;
using Microsoft.AspNetCore.Mvc;
using ResultWrapper.Library;
using WebCore.Controller;

namespace WebApi.Controllers.Course;

[ApiController]
[Route("lessons")]
[RoleAuthorize(EnumRole.User)]
[ApiExplorerSettings(GroupName = "Course")]
public class LessonController(LessonService service, CourseService courseService) : AuthorizedController
{
    [HttpGet]
    public async Task<Wrapper> GetAll([FromQuery] DataQueryRequest q, long? courseId = null) =>
        await service.GetAll(q, courseId);

    [HttpPost]
    public async Task<Wrapper> CreateOrUpdate(CreateOrUpdateLessonDto dto) => (await service.CrateOrUpdate(dto), 200);

    [HttpPut("finish/{lessonId:long:min(1)}")]
    public async Task<Wrapper> Finish(long lessonId)
    {
        await courseService.FinishEntity(this.UserId, lessonId, EnumHistoryEntityType.Lesson);
        return 200;
    }

    [HttpDelete("{lessonId:long:min(1)}")]
    public async Task<Wrapper> Remove(long lessonId)
    {
        await service.Remove(lessonId);
        return 200;
    }
}