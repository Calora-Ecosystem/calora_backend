using Core.Exceptions;

namespace Core.Services.Ai.Exceptions;

public class InvalidMetaException(string message = "unable_to_prepare_meta") : BadRequestException(message);