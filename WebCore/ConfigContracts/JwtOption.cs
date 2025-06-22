namespace WebCore.ConfigContracts;

public class JwtOption
{
    public string Issuer { get; set; }
    public string Audience { get; set; }
    //Encryption key
    public string SecretKey { get; set; }
    //Access token expire time in minutes
    public int ExpireTimeInMinutes { get; set; }
    //Refresh token expire time
    public int RExpireTimeInDays { get; set; }
    
}