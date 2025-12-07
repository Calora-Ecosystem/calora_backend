using BRB.Core.File;
using Calora.Api.Controllers;
using Core;
using Core.Brokers.DbContext;
using Core.Brokers.GeminiBroker;
using WebCore;

var builder = WebApplication.CreateBuilder(args);

builder.ConfigureDefaults("WebApi");

builder
    .AddCore()
    .AddDefaultConfiguredDbContext<AppDbContext>()
    .Services
    .AddFileService()
    .AddGeminiAi()
    ;
builder.Services.AddHttpClient();
builder.Services.AddScoped<IEmailService, SmtpEmailService>();
#if !DEBUG
builder.AddSwaggerServer("/api/");
#endif

var app = builder.Build();

app.ConfigureDefaults();

app.Run();