using WebCore;

var builder = WebApplication.CreateBuilder(args);

builder.ConfigureDefaults("WebApi");

var app = builder.Build();

app.ConfigureDefaults();

app.Run();