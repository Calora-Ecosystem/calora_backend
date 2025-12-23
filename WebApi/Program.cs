using BRB.Core.File;
using Calora.Api.Controllers;
using Core;
using Core.Brokers.DbContext;
using Core.Brokers.FirebaseBroker;
using Core.Brokers.GeminiBroker;
using Hangfire;
using Hangfire.Dashboard;
using WebCore;
using WebCore.Filters.Hangfire;

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
app.UseHangfireDashboard(options: new DashboardOptions()
{
#if !DEBUG
    PrefixPath = "/api",
#endif
    Authorization = [new Authorization(app.Environment)]
});
app.AddRecurringJobs();

app.Run();