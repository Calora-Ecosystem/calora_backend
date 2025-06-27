namespace Core.Services.Auth.Contracts;

public class AuthConfig
{
    public string SecretKey { get; set; } = null!;
    public int ATokenExpireInHours { get; set; }
    public int RTokenExpireInDays { get; set; }
    public string Issuer { get; set; } = null!;
    public string Audience { get; set; } = null!;
}