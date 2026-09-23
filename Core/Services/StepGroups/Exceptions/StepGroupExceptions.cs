namespace Core.Services.StepGroups.Exceptions;

public class StepGroupNotFoundException() : Core.Exceptions.NotFoundException("step_group_not_found");

public class StepGroupOwnerOnlyException() : Core.Exceptions.ForbiddenException("step_group_owner_only");

public class StepGroupFullException() : Core.Exceptions.BadRequestException("step_group_full");

public class StepGroupLimitReachedException() : Core.Exceptions.BadRequestException("step_group_limit_reached");

public class StepGroupMemberNotFoundException() : Core.Exceptions.NotFoundException("step_group_member_not_found");

public class StepGroupCannotRemoveOwnerException() : Core.Exceptions.BadRequestException("step_group_cannot_remove_owner");
