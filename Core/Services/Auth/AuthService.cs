using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using BRB.Core.Common.Extensions;
using Core.Services.Auth.Exceptions;
using BRB.Core.Common.Helpers;
using BRB.Core.EF.Attributes;
using BRB.Core.EF.Extensions;
using Core.Brokers.Apple;
using Core.Brokers.DbContext;
using Core.Constants;
using Core.Entities.Auth;
using Core.Enums;
using Core.Exceptions;
using Core.Helpers;
using Core.Services.Auth.Contracts;
using Core.Services.Auth.Enums;
using Core.Services.Common;
using Core.Services.Crm;
using Core.Services.Crm.Enum;
using Core.Services.Notification;
using Core.Services.Notification.Contracts;
using Google.Apis.Auth;
using Hangfire;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using ForbiddenException = BRB.Core.Common.Exceptions.ForbiddenException;

namespace Core.Services.Auth;

[Injectable]
public class AuthService(
    AppDbContext dbContext,
    MemoryCacheManager memoryCache,
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
                       .FirstOrDefaultAsync(x => x.Email != null && EF.Functions.ILike(x.Email, payload.Email)) ??
                   dbContext.Add(new Entities.Auth.User()
                   {
                       Name = payload.Name,
                       Email = payload.Email,
                       Roles = [nameof(EnumRole.User)]
                   }).Entity;

        var hasNewUser = user.Id == 0;

        await dbContext.SaveChangesAsync();

        if (hasNewUser)
            BackgroundJob.Enqueue<LeadService>(service => service.HandleEventAsync(new Core.Services.Crm.Contracts.HandleLeadEventDto(user.Id, EnumLeadEvent.Registered)));

        return await GenerateTokens(user, dto.DeviceInfo, hasNewUser);
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
            throw new InvalidTokenException();

        var kid = parts[0].GetProperty("kid").GetString() ?? throw new InvalidTokenException();
        var exp = parts[1].GetProperty("exp").GetInt64();
        var email = parts[1].GetProperty("email").GetString() ?? throw new InvalidTokenException();
        var emailVerified = parts[1].GetProperty("email_verified").GetBoolean();
        var aud = parts[1].GetProperty("aud").GetString() ?? throw new InvalidTokenException();
        var iss = parts[1].GetProperty("iss").GetString() ?? throw new InvalidTokenException();

        if (aud != "ai.calora.app")
            throw new InvalidAudienceException();

        if (!iss.EndsWith("appleid.apple.com"))
            throw new InvalidIssuerException();

#if !DEBUG
        var expDate = DateTimeOffset.FromUnixTimeSeconds(exp);
        Debug.WriteLine(expDate);

        if (expDate <= DateTime.Now)
            throw new TokenExpiredException();
#endif

        if (jwkSet.Keys.All(x => x.KeyId != kid))
            throw new InvalidTokenKidException();

        if (email.IsNullOrEmpty() || !emailVerified)
            throw new RequiredClaimPrincipalNotFoundException();

        var user = await dbContext.Users
            .FirstOrDefaultAsync(x => x.Email != null && EF.Functions.ILike(x.Email, email)) ?? new Entities.Auth.User()
        {
            Name = "Anonymous",
            Email = email,
            Roles = [nameof(EnumRole.User)]
        };

        var hasNewUser = user.Id == 0;

        if (hasNewUser)
            user = dbContext.Users.Add(user).Entity;
        else
            user = dbContext.Users.Update(user).Entity;
        await dbContext.SaveChangesAsync();

        if (hasNewUser)
            BackgroundJob.Enqueue<LeadService>(service => service.HandleEventAsync(new Core.Services.Crm.Contracts.HandleLeadEventDto(user.Id, EnumLeadEvent.Registered)));

        return await GenerateTokens(user, dto.DeviceInfo, hasNewUser);
    }

    public async Task<object> RegisterViaEmailAsync(RegisterViaEmailDto dto)
    {
        var userExists = await dbContext.Users.AnyAsync(x => x.Email != null && EF.Functions.ILike(x.Email, dto.Email));
        if (userExists)
            throw new UserAlreadyExistsException();

        var user = new Entities.Auth.User()
        {
            Name = "Anonymous",
            Email = dto.Email,
            Roles = [nameof(EnumRole.User)]
        };

        user = dbContext.Users.Add(user).Entity;

        await dbContext.SaveChangesAsync();

        return await this.SendVerificationCode(EnumChannel.Email, user);
    }

    public async Task<object> RegisterViaPhoneAsync(RegisterViaPhoneDto dto)
    {
        var validPhone = FormatHelper.MakeValidPhone(dto.Phone);

        var userExists = await dbContext.Users.AnyAsync(x => x.Phone == validPhone);
        if (userExists)
            throw new UserAlreadyExistsException();

        var user = new Entities.Auth.User()
        {
            Name = "Anonymous",
            Phone = dto.Phone,
            Roles = [nameof(EnumRole.User)]
        };

        user = dbContext.Users.Add(user).Entity;

        await dbContext.SaveChangesAsync();

        return await this.SendVerificationCode(EnumChannel.Phone, user);
    }

    public async Task<object> SignInViaEmailAsync(SignInViaEmailDto dto)
    {
        VerifyOtp(dto.VerificationCode.ToString(), dto.Code);

        var user = await dbContext.Users
                       .FirstOrDefaultAsync(x => x.Email != null && EF.Functions.ILike(x.Email, dto.Email)) ??
                   new Entities.Auth.User()
                   {
                       Name = "Anonymous",
                       Email = dto.Email,
                       Roles = [nameof(EnumRole.User)]
                   };

        var hasNewUser = user.Id == 0;

        user = dbContext.Users.Update(user).Entity;
        await dbContext.SaveChangesAsync();
        
        if (hasNewUser)
            BackgroundJob.Enqueue<LeadService>(service => service.HandleEventAsync(new Core.Services.Crm.Contracts.HandleLeadEventDto(user.Id, EnumLeadEvent.Registered)));

        return await GenerateTokens(user, dto.DeviceInfo, hasNewUser);
    }

    public async Task<object> SignInViaPhoneAsync(SignInViaPhoneDto dto)
    {
        var validPhone = FormatHelper.MakeValidPhone(dto.Phone);
        VerifyOtp(dto.VerificationCode.ToString(), dto.Code);
        var user = await dbContext.Users
            .FirstOrDefaultAsync(x => x.Phone == validPhone) ?? new Entities.Auth.User()
        {
            Name = "Anonymous",
            Phone = validPhone,
            Roles = [nameof(EnumRole.User)]
        };

        var hasNewUser = user.Id == 0;

        user = dbContext.Users.Update(user).Entity;
        await dbContext.SaveChangesAsync();
        
        if (hasNewUser)
            BackgroundJob.Enqueue<LeadService>(service => service.HandleEventAsync(new Core.Services.Crm.Contracts.HandleLeadEventDto(user.Id, EnumLeadEvent.Registered)));

        return await GenerateTokens(user, dto.DeviceInfo, hasNewUser);
    }

    public void VerifyOtp(string verificationCode, string code)
    {
        if (!memoryCache.TryGetValue(verificationCode, out string? otp))
            throw new OtpExpiredException();

        memoryCache.Remove(verificationCode);

        if (environment.IsProduction())
            if (code.IsNullOrEmpty() || otp.IsNullOrEmpty() || otp != code)
                throw new InvalidOtpException();
            else ;
        else if (code != "777777")
            throw new InvalidOtpException();
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

    public async Task<object> SendVerificationCode(EnumChannel channel, Entities.Auth.User user)
    {
        return await this.SendVerificationCode(channel, (channel switch
        {
            EnumChannel.Email => user.Email,
            EnumChannel.Phone => user.Phone,
            _ => throw new InvalidChannelException()
        })!);
    }

    public async Task<object> SendVerificationCode(EnumChannel channel, string destination)
    {
        var expireDate = DateTime.Now.AddMinutes(2);
        var code = Guid.NewGuid().ToString();
        var otp = environment.IsProduction()
            ? Random.Shared.Next(100_000, 999_999).ToString()
            : "777777";

        memoryCache.Set(code, otp, expireDate);

        if (channel == EnumChannel.Email)
            await notificationService.SendMailAsync(new EmailNotificationWithoutUserDto()
            {
                Email = destination,
                Title = "Verification Code",
                Description = MessageTemplates.MakeMessage(MessageTemplates.OtpSign, otp)
            });
        else
        {
            if (
#if DEBUG
                true ||
#endif
                environment.IsProduction())
                await notificationService.SendSms(new SmsNotificationDto()
                {
                    Phone = destination,
                    Title = "Verification Code", // title doesn't sent. it is only for log
                    Description = MessageTemplates.MakeMessage(MessageTemplates.OtpSign, otp)
                });
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
            && x.RToken == rToken) ?? throw new UserOrRefreshTokenNotFoundException();

        if (!await dbContext.Devices.AnyAsync(x => x.Id == deviceId && x.UserId == userId && x.IsActive))
            throw new DeviceNotFoundException();

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
        var user = await dbContext.Users
            .GetByIdOrThrowsNotFoundException(userId);

        var subscription = await dbContext.Subscriptions.FirstOrDefaultAsync(x => x.UserId == user.Id && x.IsActive);

        var claims = new List<Claim>();

        var sessionId = Guid.NewGuid().ToString();

        user.Roles.ForEach(role => claims.Add(new Claim(ClaimTypes.Role, role)));
        // claims.Add(new Claim(ClaimTypes.Email, user.Email));
        claims.Add(new Claim(CustomClaims.DeviceId, deviceId.ToString()));
        claims.Add(new Claim(CustomClaims.UserId, user.Id.ToString()));
        claims.Add(new Claim(CustomClaims.SessionId, sessionId));
        claims.Add(new Claim(CustomClaims.Plan,
            (subscription?.SubscriptionPlan ?? EnumSPlans.Free).ToString().ToLowerInvariant()));

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

        memoryCache.RemoveByPrefix($"session:{user.Id}:");
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
                     throw new MissingClaimException();

        var deviceId = claims.FirstOrDefault(x => x.Type == CustomClaims.DeviceId)?.Value;

        var session = claims.FirstOrDefault(x => x.Type == CustomClaims.SessionId)?.Value ??
                      throw new MissingClaimException();

        var user = await dbContext.Users.GetByIdOrThrowsNotFoundException(long.Parse(userId));

        memoryCache.Remove($"session:{user.Id}:{session}");

        if (!deviceId.IsNullOrEmpty())
        {
            var id = long.Parse(deviceId!);

            var device = dbContext.Devices.FirstOrDefault(x => x.Id == id && x.UserId == user.Id);

            if (device is not null)
            {
                device.IsActive = false;
            }
        }

        user.RToken = null;
        user.RTokenExpireAt = DateTime.MinValue;

        await dbContext.SaveChangesAsync();
    }

    public Task KillAllUserSessions(long userId)
    {
        memoryCache.RemoveByPrefix($"session:{userId}:");
        return Task.CompletedTask;
    }

    public async Task KillUser(long authUserId, long userId)
    {
        if (authUserId != userId)
            throw new ForbiddenException();

        var user = await dbContext.Users
            .IgnoreQueryFilters()
            .GetByIdOrThrowsNotFoundException(userId);

        user.IsDeleted = true;
        user.RToken = null;
        user.RTokenExpireAt = DateTime.MinValue;

        dbContext.Users.Update(user);

        await dbContext.Devices
            .Where(x => x.UserId == userId && x.IsActive)
            .ExecuteUpdateAsync(x => x.SetProperty(d => d.IsActive, false));

        await dbContext.SaveChangesAsync();

        await KillAllUserSessions(userId);
    }
}