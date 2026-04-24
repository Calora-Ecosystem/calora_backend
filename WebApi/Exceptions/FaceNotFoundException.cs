namespace WebApi.Exceptions;

public class FaceNotFoundException() : Core.Exceptions.BadRequestException("face_not_found");
