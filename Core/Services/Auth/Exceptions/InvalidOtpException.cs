namespace Core.Services.Auth.Exceptions;

public class InvalidOtpException() : Core.Exceptions.NotFoundException("invalid_otp");
