using BRB.Core.Common.Models;
using Core;
using Core.Entities.Course.Enum;
using Core.Enums;
using Core.Services.Course.Course;
using Core.Services.Course.Workout;
using Core.Services.Course.Workout.Contracts;
using Microsoft.AspNetCore.Mvc;
using ResultWrapper.Library;
using WebCore.Controller;

namespace WebApi.Controllers.Course;

[ApiController]
[Route("exercises")]
[RoleAuthorize(EnumRole.User)]
[ApiExplorerSettings(GroupName = "Course")]
public class ExerciseController(WorkoutService service, CourseService courseService) : AuthorizedController
{
    [HttpGet]
    public async Task<Wrapper> GetAll([FromQuery] DataQueryRequest q, long workoutId) =>
        await service.GetAllExercises(this.UserId, workoutId, q);

    [HttpPost]
    public async Task<Wrapper> CreateOrUpdate(CreateOrUpdateExerciseDto dto) =>
        (await service.CrateOrUpdateExercise(dto), 200);

    [HttpPut("finish/{exerciseId:long:min(1)}")]
    public async Task<Wrapper> Finish(long exerciseId)
    {
        await courseService.FinishEntity(this.UserId, exerciseId, EnumHistoryEntityType.Exercise);
        return 200;
    }

    [HttpDelete("{exerciseId:long:min(1)}")]
    public async Task<Wrapper> Remove(long exerciseId)
    {
        await service.Remove(exerciseId);
        return 200;
    }
}