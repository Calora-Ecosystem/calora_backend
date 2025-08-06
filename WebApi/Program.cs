using BRB.Core.File;
using Core;
using Core.Brokers.DbContext;
using WebCore;

var builder = WebApplication.CreateBuilder(args);

builder.ConfigureDefaults("WebApi");

builder
    .AddCore()
    .AddDefaultConfiguredDbContext<AppDbContext>()
    .Services
    .AddFileService()
    ;
builder.Services.AddHttpClient();

#if !DEBUG
builder.AddSwaggerServer("/api/");
#endif

var app = builder.Build();

app.ConfigureDefaults();

app.Run();