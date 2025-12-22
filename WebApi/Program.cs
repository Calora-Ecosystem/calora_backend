using BRB.Core.File;
using Calora.Api.Controllers;
using Core;
using Core.Brokers.DbContext;
using Core.Brokers.FirebaseBroker;
using Core.Brokers.GeminiBroker;
using Hangfire;
using WebCore;

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
builder.Services.AddHttpClient();
builder.Services.AddScoped<IEmailService, SmtpEmailService>();
#if !DEBUG
builder.AddSwaggerServer("/api/");
#endif

builder
    .AddHangfireDefault();

var app = builder.Build();

app.ConfigureDefaults();
app.UseHangfireDashboard();
app.AddRecurringJobs();

app.Run();