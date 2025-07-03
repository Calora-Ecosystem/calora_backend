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

var app = builder.Build();

app.ConfigureDefaults();

app.Run();