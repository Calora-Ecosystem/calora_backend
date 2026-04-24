namespace Core.Services.FoodService.Exceptions;

public class AuthorizedUserRequiredException() : Core.Exceptions.BadRequestException("authorized_user_required");
