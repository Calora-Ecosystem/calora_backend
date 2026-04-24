namespace Core.Services.Auth.Exceptions;

public class UserOrRefreshTokenNotFoundException() : Core.Exceptions.NotFoundException("user_or_refresh_token_not_found");
