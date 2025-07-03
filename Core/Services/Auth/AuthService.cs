using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BRB.Core.Common.Exceptions;
using BRB.Core.Common.Extensions;
using BRB.Core.Common.Helpers;
using BRB.Core.EF.Attributes;
using BRB.Core.EF.Extensions;
using Core.Brokers.DbContext;
using Core.Brokers.EmailBroker;
using Core.Constants;
using Core.Entities.Auth;
using Core.Enums;
using Core.Services.Auth.Contracts;
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
    EmailClient emailClient,
    IMemoryCache memoryCache,
    IWebHostEnvironment environment,
    DeviceService deviceService,
    IOptions<AuthConfig> authConfig)
{
    public async Task<object> RegisterAsync(RegisterDto dto)
    {
        var userExists = await dbContext.Users.AnyAsync(x => EF.Functions.ILike(x.Email, dto.Email));
        if (userExists)
            throw new AlreadyExistsException("User already exists");

        var user = new User()
        {
            Email = dto.Email
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
                       .FirstOrDefaultAsync(x => x.Email == dto.Email) ??
                   throw new NotFoundException("User not found");

        Device? device = null;

        await dbContext.Transactional(async () =>
        {
            device = await deviceService.CreateOrUpdateDeviceAndGet(user.Id, dto.DeviceInfo);
            await LogSignInfo(user.Id, device.Id);
        });

        var accessToken = MakeJwtFromUser(user.Id, device!.Id);
        var refreshToken = PasswordHelper.Encrypt(Guid.NewGuid().ToString());

        user.RToken = refreshToken;
        user.RTokenExpireAt = DateTime.Now.AddDays(authConfig.Value.RTokenExpireInDays);

        user = dbContext.Users.Update(user).Entity;
        await dbContext.SaveChangesAsync();

        return new
        {
            AccessToken = accessToken,
            RefreshToken = user.RToken,
            RefreshTokenExpireAt = user.RTokenExpireAt
        };
    }

    public async Task<object> SendVerificationCode(string email)
    {
        return this.SendVerificationCode(
            await dbContext.Users.FirstOrDefaultAsync(x => EF.Functions.ILike(x.Email, email)) ??
            throw new NotFoundException("User not found"));
    }

    public async Task<object> SendVerificationCode(User user)
    {
        var expireDate = DateTime.Now.AddMinutes(2);
        var code = Guid.NewGuid().ToString();
        var otp = environment.IsProduction()
            ? Random.Shared.Next(100_000, 999_999).ToString()
            : "777777";

        memoryCache.Set(code, otp, expireDate);

        try
        {
            await emailClient.SendMailAsync(user.Email, MessageTemplates.MakeMessage(MessageTemplates.OtpSign, otp));
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

    public IEnumerable<string> GetAllRoles()
    {
        return Enum.GetValues<EnumRole>().Select(x => x.ToString());
    }

    private async Task<string> MakeJwtFromUser(long userId, long deviceId)
    {
        var user = await dbContext.Users.GetByIdOrThrowsNotFoundException(userId);

        var claims = new List<Claim>();

        claims.Add(new Claim(ClaimTypes.Role, string.Join(",", user.Roles)));
        claims.Add(new Claim(ClaimTypes.Email, user.Email));
        claims.Add(new Claim(CustomClaims.DeviceId, deviceId.ToString()));

        var token = new JwtSecurityToken(authConfig.Value.Issuer,
            authConfig.Value.Audience,
            claims,
            expires: DateTime.Now.AddHours(authConfig.Value.ATokenExpireInHours),
            signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(authConfig.Value.SecretKey)),
                SecurityAlgorithms.HmacSha256));

        var hash = new JwtSecurityTokenHandler().WriteToken(token);

        return hash;
    }

    private async Task LogSignInfo(long userId, long deviceId)
    {
        dbContext.SignLogs.Add(new SignLog() { UserId = userId, DeviceId = deviceId, SignAt = DateTime.Now });
        await dbContext.SaveChangesAsync();
    }
}