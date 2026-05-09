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
    .AddClickService()
    ;

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.OnRejected = async (context, ct) =>
    {
        await context.HttpContext.Response.WriteAsJsonAsync(new Wrapper()
        {
            Content = TimeSpan.FromHours(6),
            Code = HttpStatusCode.TooManyRequests,
            Error = "too many requests"
        }, ct);
    };

    options.AddPolicy("otp_limit",
        context =>
        {
            var userId =
                (context.User.Identity is { IsAuthenticated: true } ? context.User?.FindFirst(CustomClaims.UserId)?.Value : null)
                ?? (context.Request.Headers["X-Real-IP"].Count > 0
                    ? context.Request.Headers["X-Real-IP"].ToString()
                    : null)
                ?? "anonymous";

            return RateLimitPartition.GetFixedWindowLimiter(userId, s => new FixedWindowRateLimiterOptions()
            {
                AutoReplenishment = true,
                PermitLimit = builder.Environment.IsProduction() ? 3 : 100,
                Window = TimeSpan.FromHours(6),
            });
        }
    );
});

builder.Services.AddScoped<IEmailService, SmtpEmailService>();
builder.Services.AddHttpClient();
#if !DEBUG
builder.AddSwaggerServer("/api/", "Staging API");
builder.AddSwaggerServer("https://calora.uz/api/", "Production API");
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