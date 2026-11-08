namespace Core.Services.FoodService.Exceptions;

public class EmptyRecognitionFileException() : Core.Exceptions.BadRequestException("file_is_empty");
