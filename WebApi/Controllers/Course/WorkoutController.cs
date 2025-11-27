using BRB.Core.Common.Models;
using Core;
using Core.Entities.Course;
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
[Route("workouts")]
[RoleAuthorize(EnumRole.User)]
[ApiExplorerSettings(GroupName = "Course")]
public class WorkoutController(WorkoutService service, CourseService courseService) : AuthorizedController
{
    [HttpGet]
    [ProducesResponseType(typeof(WrapperGeneric<GetWorkoutDto>), 200)]
    public async Task<Wrapper> GetAll([FromQuery] DataQueryRequest q) => await service.GetAll(this.UserId, q);
    
    [HttpGet("computations")]
    [ProducesResponseType(typeof(WrapperGeneric<ComputationDto>), 200)]
    public async Task<Wrapper> GetAllComputations(long workoutId) =>
        (await service.GetWorkoutComputations(workoutId), 200);

    // [HttpGet("/{courseId:long:min(1)}/workouts")]
    // [ProducesResponseType(typeof(WrapperGeneric<GetWorkoutDto>), 200)]
    // public async Task<Wrapper> GetAllByCourseId([FromQuery] DataQueryRequest q, [FromRoute] long courseId) =>
    //     await service.GetAll(this.UserId, q, courseId);

    [HttpPost]
    [RoleAuthorize(EnumRole.SuperAdmin)]
    public async Task<Wrapper> CreateOrUpdate(CreateOrUpdateWorkoutDto dto) => (await service.CrateOrUpdate(dto), 200);

    [HttpPut("finish/{workoutId:long:min(1)}")]
    public async Task<Wrapper> Finish(long workoutId)
    {
        await courseService.FinishEntity(this.UserId, workoutId, EnumEntityType.Workout);
        return 200;
    }

    [HttpPut("reset/{workoutId:long:min(1)}")]
    public async Task<Wrapper> Reset(long workoutId)
    {
        await service.ResetWorkout(this.UserId, workoutId);
        return 200;
    }

    [HttpDelete("{workoutId:long:min(1)}")]
    [RoleAuthorize(EnumRole.SuperAdmin)]
    public async Task<Wrapper> Remove(long workoutId)
    {
        await service.Remove(workoutId);
        return 200;
    }
}