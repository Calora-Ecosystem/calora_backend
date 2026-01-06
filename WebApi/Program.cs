using System.Net;
using System.Threading.RateLimiting;
using BRB.Core.File;
using Calora.Api.Controllers;
using Core;
using Core.Brokers.DbContext;
using Core.Brokers.FirebaseBroker;
using Core.Brokers.GeminiBroker;
using Core.Constants;
using Hangfire;
using ResultWrapper.Library;
using WebCore;
using Authorization = WebCore.Filters.Hangfire.Authorization;

var builder = WebApplication.CreateBuilder(args);

builder.ConfigureDefaults("WebApi");

builder
    .AddCore()
    .AddDefaultConfiguredDbContext<AppDbContext>()
    .Services
    .AddFileService()
    .AddGeminiAi()
    .AddFirebaseAdmin()
    ;

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.OnRejected = async (context, ct) =>
    {
        await context.HttpContext.Response.WriteAsJsonAsync(Wrapper.ResultFromContent("too many requests",
            HttpStatusCode.TooManyRequests), ct);
    };

    options.AddPolicy("otp_limit",
        context =>
        {
            var userId =
                context.User?.FindFirst(CustomClaims.UserId)?.Value
                ?? context.Connection.RemoteIpAddress?.ToString()
                ?? "anonymous";
            
            return RateLimitPartition.GetFixedWindowLimiter(userId, s => new FixedWindowRateLimiterOptions()
            {
                AutoReplenishment = true,
                PermitLimit = 3,
                Window = TimeSpan.FromHours(6),
            });
        }
    );
});

builder.Services.AddHttpClient();
builder.Services.AddScoped<IEmailService, SmtpEmailService>();
#if !DEBUG
builder.AddSwaggerServer("/api/");
#endif

builder
    .AddHangfireDefault();

var app = builder.Build();

app.UseRateLimiter();

app.ConfigureDefaults();

app.UseHangfireDashboard(options: new DashboardOptions()
{
#if !DEBUG
    PrefixPath = "/api",
#endif
    Authorization = [new Authorization(app.Environment)]
});
app.AddRecurringJobs();

app.Run();