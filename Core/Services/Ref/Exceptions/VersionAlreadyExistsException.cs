namespace Core.Services.Ref.Exceptions;

public class VersionAlreadyExistsException() : Core.Exceptions.BadRequestException("version_already_exists");
