using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using BRB.Core.Common.Exceptions;
using BRB.Core.Common.Extensions;
using BRB.Core.Common.Helpers;
using BRB.Core.EF.Attributes;
using BRB.Core.EF.Extensions;
using Core.Brokers.Apple;
using Core.Brokers.DbContext;
using Core.Brokers.EmailBroker;
using Core.Constants;
using Core.Entities.Auth;
using Core.Enums;
using Core.Services.Auth.Contracts;
using Core.Services.Notification;
using Core.Services.Notification.Contracts;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Serilog;

namespace Core.Services.Auth;

[Injectable]
public class AuthService(
    AppDbContext dbContext,
    IMemoryCache memoryCache,
    IWebHostEnvironment environment,
    DeviceService deviceService,
    IOptions<AuthConfig> authConfig,
    NotificationService notificationService,
    AppleClient appleClient
)
{
    public async Task<object> SignInWithGoogle(SsoSignInDto dto)
    {
        var payload = await GoogleJsonWebSignature.ValidateAsync(dto.SsoToken);

        var user = await dbContext.Users
            .FirstOrDefaultAsync(x => EF.Functions.ILike(x.Email, payload.Email)) ?? new Entities.Auth.User()
        {
            Name = payload.Name,
            Email = payload.Email,
            Roles = [nameof(EnumRole.User)]
        };

        var hasNewUser = user.Id == 0;

        user = dbContext.Users.Update(user).Entity;
        await dbContext.SaveChangesAsync();

        return GenerateTokens(user, dto.DeviceInfo, hasNewUser);
    }

    public async Task<object> SignInWithAppleToken(SsoSignInDto dto)
    {
        var jwkSet = await appleClient.FetchAppleJwkSet();

        Debug.WriteLine(jwkSet);

        var parts = dto.SsoToken
            .Split(".")
            .Take(2)
            .Select(x => JsonSerializer.Deserialize<JsonElement>(Base64UrlEncoder.Decode(x)))
            .ToArray();

        if (parts.Length < 2)
            throw new UnauthorizedException("Invalid token");

        var kid = parts[0].GetProperty("kid").GetString() ?? throw new UnauthorizedException("Invalid token");
        var exp = parts[1].GetProperty("exp").GetInt64();
        var email = parts[1].GetProperty("email").GetString() ?? throw new UnauthorizedException("Invalid token");
        var emailVerified = parts[1].GetProperty("email_verified").GetBoolean();
        var aud = parts[1].GetProperty("aud").GetString() ?? throw new UnauthorizedException("Invalid token");
        var iss = parts[1].GetProperty("iss").GetString() ?? throw new UnauthorizedException("Invalid token");

        if (aud != "uz.zingo.app")
            throw new UnauthorizedException("Invalid audience");

        if (!iss.EndsWith("appleid.apple.com"))
            throw new UnauthorizedException("Invalid issuer");

#if !DEBUG
        var expDate = DateTimeOffset.FromUnixTimeSeconds(exp);
        Debug.WriteLine(expDate);
        
        if (expDate <= DateTime.Now)
            throw new UnauthorizedException("Token expired");
#endif

        if (jwkSet.Keys.All(x => x.KeyId != kid))
            throw new UnauthorizedException("Invalid token kid");

        if (email.IsNullOrEmpty() || !emailVerified)
            throw new UnauthorizedException("Required claim principal not found");

        var user = await dbContext.Users
            .FirstOrDefaultAsync(x => EF.Functions.ILike(x.Email, email)) ?? new Entities.Auth.User()
        {
            Name = "Anonymous",
            Email = email,
            Roles = [nameof(EnumRole.User)]
        };

        var hasNewUser = user.Id == 0;

        user = dbContext.Users.Update(user).Entity;
        await dbContext.SaveChangesAsync();

        return GenerateTokens(user, dto.DeviceInfo, hasNewUser);
    }

    public async Task<object> RegisterAsync(RegisterDto dto)
    {
        var userExists = await dbContext.Users.AnyAsync(x => EF.Functions.ILike(x.Email, dto.Email));
        if (userExists)
            throw new AlreadyExistsException("User already exists");

        var user = new Entities.Auth.User()
        {
            Name = "Anonymous",
            Email = dto.Email,
            Roles = [nameof(EnumRole.User)]
        };

        user = dbContext.Users.Add(user).Entity;

        await dbContext.SaveChangesAsync();

        return await this.SendVerificationCode(user);
    }

    public async Task<object> SignInAsync(SignInDto dto)
    {
        if (!memoryCache.TryGetValue(dto.VerificationCode.ToString(), out string? otp))
            throw new NotFoundException("Otp not found or expired");

        memoryCache.Remove(dto.VerificationCode.ToString());

        if (environment.IsProduction())
            if (dto.Code.IsNullOrEmpty() || otp.IsNullOrEmpty() || otp != dto.Code)
                throw new NotFoundException("Otp didn't match");
            else ;
        else if (dto.Code != "777777")
            throw new NotFoundException("Otp didn't match");

        var user = await dbContext.Users
            .FirstOrDefaultAsync(x => EF.Functions.ILike(x.Email, dto.Email)) ?? new Entities.Auth.User()
        {
            Name = "Anonymous",
            Email = dto.Email,
            Roles = [nameof(EnumRole.User)]
        };

        var hasNewUser = user.Id == 0;

        user = dbContext.Users.Update(user).Entity;
        await dbContext.SaveChangesAsync();

        return GenerateTokens(user, dto.DeviceInfo, hasNewUser);
    }

    public async Task<object> GenerateTokens(Entities.Auth.User user, DeviceDto deviceInfo, bool hasNewUser)
    {
        Device? device = null;

        await dbContext.Transactional(async () =>
        {
            device = await deviceService.CreateOrUpdateDeviceAndGet(user.Id, deviceInfo);
            await LogSignInfo(user.Id, device.Id);
        });

        var accessToken = await MakeJwtFromUser(user.Id, device!.Id);
        var refreshToken = PasswordHelper.Encrypt(Guid.NewGuid().ToString());

        user.RToken = refreshToken;
        user.RTokenExpireAt = DateTime.Now.AddDays(authConfig.Value.RTokenExpireInDays);

        user = dbContext.Users.Update(user).Entity;
        await dbContext.SaveChangesAsync();

        return new
        {
            AccessToken = accessToken,
            RefreshToken = user.RToken,
            RefreshTokenExpireAt = user.RTokenExpireAt,
            hasNewUser,
        };
    }

    public async Task<object> SendVerificationCode(Entities.Auth.User user)
    {
        return await this.SendVerificationCode(user.Email);
    }

    public async Task<object> SendVerificationCode(string email)
    {
        var expireDate = DateTime.Now.AddMinutes(2);
        var code = Guid.NewGuid().ToString();
        var otp = environment.IsProduction()
            ? Random.Shared.Next(100_000, 999_999).ToString()
            : "777777";

        memoryCache.Set(code, otp, expireDate);

        try
        {
            await notificationService.SendMailAsync(new EmailNotificationWithoutUserDto()
            {
                Email = email,
                Title = "Verification Code",
                Description = MessageTemplates.MakeMessage(MessageTemplates.OtpSign, otp)
            });
        }
        catch (Exception e)
        {
            Log.Error("Mail Sending Error: {0}", e);
        }

        return new
        {
            VerificationCode = code,
            ExpireDate = expireDate
        };
    }

    public async Task<object> RefreshToken(long userId, string rToken, long deviceId)
    {
        var user = await dbContext.Users.FirstOrDefaultAsync(x =>
            x.Id == userId
            && x.RTokenExpireAt > DateTime.Now
            && x.RToken == rToken) ?? throw new NotFoundException("User or refresh token not found");

        var accessToken = await MakeJwtFromUser(user.Id, deviceId);
        var refreshToken = PasswordHelper.Encrypt(Guid.NewGuid().ToString());

        user.RToken = refreshToken;
        user.RTokenExpireAt = DateTime.Now.AddDays(authConfig.Value.RTokenExpireInDays);

        user = dbContext.Users.Update(user).Entity;
        await dbContext.SaveChangesAsync();

        return new
        {
            AccessToken = accessToken,
            RefreshToken = user.RToken,
            RefreshTokenExpireAt = user.RTokenExpireAt,
        };
    }

    public IEnumerable<string> GetAllRoles()
    {
        return Enum.GetValues<EnumRole>().Select(x => x.ToString());
    }

    private async Task<string> MakeJwtFromUser(long userId, long deviceId)
    {
        var user = await dbContext.Users.GetByIdOrThrowsNotFoundException(userId);

        var claims = new List<Claim>();

        var sessionId = Guid.NewGuid().ToString();

        user.Roles.ForEach(role => claims.Add(new Claim(ClaimTypes.Role, role)));
        claims.Add(new Claim(ClaimTypes.Email, user.Email));
        claims.Add(new Claim(CustomClaims.DeviceId, deviceId.ToString()));
        claims.Add(new Claim(CustomClaims.UserId, user.Id.ToString()));
        claims.Add(new Claim(CustomClaims.SessionId, sessionId));

        var expires = DateTime.Now.AddHours(authConfig.Value.ATokenExpireInHours);

        if (!environment.IsProduction() && user.Email == "zokirjonashiraliyev@gmail.com")
            expires = DateTime.Now.AddMinutes(1);

        var token = new JwtSecurityToken(authConfig.Value.Issuer,
            authConfig.Value.Audience,
            claims,
            expires: expires,
            signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(authConfig.Value.SecretKey)),
                SecurityAlgorithms.HmacSha256));

        memoryCache.Set($"session:{user.Id}:{sessionId}", DateTime.Now.Ticks, expires);

        var hash = new JwtSecurityTokenHandler().WriteToken(token);

        return hash;
    }

    private async Task LogSignInfo(long userId, long deviceId)
    {
        dbContext.SignLogs.Add(new SignLog() { UserId = userId, DeviceId = deviceId, SignAt = DateTime.Now });
        await dbContext.SaveChangesAsync();
    }

    public async Task Logout(Claim[] claims)
    {
        var userId = claims.FirstOrDefault(x => x.Type == CustomClaims.UserId)?.Value ??
                     throw new UnauthorizedException();
        var session = claims.FirstOrDefault(x => x.Type == CustomClaims.SessionId)?.Value ??
                      throw new UnauthorizedException();

        var user = await dbContext.Users.GetByIdOrThrowsNotFoundException(long.Parse(userId));

        memoryCache.Remove($"session:{user.Id}:{session}");

        user.RToken = null;
        user.RTokenExpireAt = DateTime.MinValue;

        dbContext.Users.Update(user);
        await dbContext.SaveChangesAsync();
    }
}