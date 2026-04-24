namespace Core.Services.Auth.Exceptions;

public class OtpExpiredException() : Core.Exceptions.NotFoundException("otp_expired");
