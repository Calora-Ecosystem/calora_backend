using Core.Exceptions;

namespace Core.Services.Notification.Exceptions;

public class MenuOnlyForFoodException(string message = "menu_only_for_food_type") : BadRequestException(message);