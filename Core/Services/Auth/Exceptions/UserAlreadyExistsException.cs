namespace Core.Services.Auth.Exceptions;

public class UserAlreadyExistsException() : Core.Exceptions.AlreadyExistsException("user_already_exists");
