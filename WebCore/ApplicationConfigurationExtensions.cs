using System.Reflection;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using BRB.Core.Web.Fallback;
using BRB.Core.Web.Filters;
using BRB.Core.Web.Middlewares;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Swashbuckle.AspNetCore.SwaggerGen;
using Swashbuckle.AspNetCore.SwaggerUI;
using WebCore.ConfigContracts;
using WebCore.Converters;
using WebCore.Filters.Swagger;

namespace WebCore;

public static class ApplicationConfigurationExtensions
{
    public static WebApplicationBuilder ConfigureDefaults(this WebApplicationBuilder builder, string appName)
    {
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

        builder
            .ConfigureKestrel()
            .ConfigureHostConfigurations(appName)
            .ConfigureLogger()
            .ConfigureSwagger(appName)
            .ConfigureControllers()
            .ConfigureCors()
            .ConfigureGlobalExceptionHandler()
            .ConfigureHealthCheck()
            .ConfigureAuth()
            .AddRpcServices()
            .AddApplicationErrors()
            .AddServices()
            .ConfigureEmail();

        return builder;
    }

    public static WebApplication ConfigureDefaults(this WebApplication app)
    {
        if (!app.Environment.IsProduction())
        {
            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.ConfigObject.AdditionalItems.Add("persistAuthorization", true);
                options.DocExpansion(DocExpansion.None);
                options.EnableDeepLinking();
            });
        }

        app.UseCors();

        app.UseMiddleware<GlobalExceptionHandlerMiddleware>();


        app.UseStaticFiles(new StaticFileOptions()
        {
            RequestPath = "/file",
            HttpsCompression = HttpsCompressionMode.Compress,
            ServeUnknownFileTypes = true,
            OnPrepareResponse = (context) =>
            {
                context.Context.Response.Headers.Append("Cache-Control", $"public,max-age={2 * 24 * 60 * 60}");
            }
        });


        app.UseHealthChecks("/healthy");
        app.UseAuthorization();
        // app.UseAuthentication();
        app.UseCustom404Page("");
        app.MapControllers();


        return app;
    }

    public static WebApplicationBuilder AddSwaggerServer(this WebApplicationBuilder builder, string url,
        string? description = null)
    {
        builder.Services.Configure<SwaggerGenOptions>(c => c.AddServer(new OpenApiServer()
        {
            Url = url,
            Description = description ?? $"{builder.Environment.EnvironmentName} Server"
        }));

        return builder;
    }


    private static WebApplicationBuilder ConfigureKestrel(this WebApplicationBuilder builder)
    {
        builder.Services.Configure<KestrelServerOptions>(options => { options.Limits.MaxRequestBodySize = null; });

        return builder;
    }

    private static WebApplicationBuilder ConfigureHostConfigurations(this WebApplicationBuilder builder,
        string appName)
    {
        _ = builder.Configuration.AddJsonFile(
            Path.Join(AppContext.BaseDirectory,
                $"appsettings.{builder.Environment.EnvironmentName}.json"),
            optional: false);
        _ = builder.Configuration.AddJsonFile(
            Path.Join(AppContext.BaseDirectory,
                $"appsettings.json"),
            optional: false);
        builder.Configuration.AddEnvironmentVariables();

        return builder;
    }

    public static WebApplicationBuilder ConfigureLogger(this WebApplicationBuilder builder)
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.ControlledBy(new LoggingLevelSwitch(LogEventLevel.Information))
            .Enrich.FromLogContext()
            .WriteTo
            .Console(LogEventLevel.Debug)
            .CreateLogger();

        Log.Information("Project started at {0} with PID: {1}",
            DateTime.Now.ToString("dd.MM.yyyy hh:mm:ss"),
            Environment.ProcessId);

        builder.Logging.ClearProviders();
        builder.Logging.AddSerilog();

        return builder;
    }


    private static WebApplicationBuilder ConfigureSwagger(this WebApplicationBuilder builder, string appName)
    {
        builder.Services.AddEndpointsApiExplorer();

        builder.Services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1",
                new OpenApiInfo()
                {
                    Title = appName,
                    Version = "v1"
                });

            options.CustomSchemaIds(type => type.FullName);
            options.OperationFilter<MlfHeaderFilter>();
            options.OperationFilter<PermissionFilter>();

            var securityScheme = new OpenApiSecurityScheme()
            {
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Description = "Cookie and Header based Authentication",
                Name = "Jwt",
                Scheme = JwtBearerDefaults.AuthenticationScheme,
                Reference = new OpenApiReference()
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = JwtBearerDefaults.AuthenticationScheme
                }
            };

            options.AddSecurityDefinition("Bearer", securityScheme);

            options.AddSecurityRequirement(new OpenApiSecurityRequirement()
            {
                {
                    securityScheme, new List<string>()
                    {
                        "Bearer"
                    }
                }
            });

            options.TagActionsBy(api =>
            {
                if (api.GroupName != null)
                {
                    return
                    [
                        api.GroupName
                    ];
                }

                if (api.ActionDescriptor is ControllerActionDescriptor controllerActionDescriptor)
                {
                    return
                    [
                        controllerActionDescriptor.ControllerName
                    ];
                }

                throw new InvalidOperationException("Unable to determine tag for endpoint.");
            });
            
            options.DocInclusionPredicate((name, api) => true);

            var filePath = Path.Combine(AppContext.BaseDirectory, $"{Assembly.GetEntryAssembly()?.GetName().Name}.xml");
            if (File.Exists(filePath))
                options.IncludeXmlComments(filePath);
        });

        builder.Services.Configure<ApiBehaviorOptions>(options => { options.SuppressModelStateInvalidFilter = true; });
        builder.Services.AddCookiePolicy(options => { options.Secure = CookieSecurePolicy.Always; });


        return builder;
    }

    private static WebApplicationBuilder ConfigureControllers(this WebApplicationBuilder builder)
    {
        IHttpContextAccessor httpContextAccessor = new HttpContextAccessor();

        builder.Services.AddSingleton(httpContextAccessor);

        builder.Services.AddControllers(options =>
        {
            options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
            options.Filters.Add<ModelValidationFilter>();
        }).AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals;
            options.JsonSerializerOptions.Converters.Add(new MultiLanguageFieldConverter(httpContextAccessor));
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        });


        return builder;
    }

    private static WebApplicationBuilder ConfigureCors(this WebApplicationBuilder builder)
    {
        builder.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(policyBuilder =>
                policyBuilder
                    .AllowCredentials()
                    .WithOrigins(builder.Configuration.GetSection("Origins").Get<string[]?>() ?? ["localhost"])
                    .AllowAnyHeader()
                    .AllowAnyMethod()
            );
        });

        return builder;
    }

    private static WebApplicationBuilder ConfigureGlobalExceptionHandler(this WebApplicationBuilder builder)
    {
        builder.Services.AddScoped<GlobalExceptionHandlerMiddleware>();
        return builder;
    }

    private static WebApplicationBuilder ConfigureAuth(this WebApplicationBuilder builder)
    {
        builder.Services
            .AddOptions<JwtOption>()
            .BindConfiguration("Auth")
            .ValidateOnStart();

        builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters()
                {
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero,
                    SaveSigninToken = true,
                    RoleClaimType = ClaimTypes.Role,
                    IssuerSigningKey =
                        new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Auth:SecretKey"]!)),
                };

                // options.Events = new JwtBearerEvents
                // {
                //     OnMessageReceived = context =>
                //     {
                //         context.Token = context.Request.Cookies[Constant.Constants.ACCESS_TOKEN_KEY];
                //
                //         if (!builder.Environment.IsProduction() && context.Token.IsNullOrEmpty() &&
                //             context.Request.Headers.Authorization.Count > 0)
                //             context.Token = context.Request.Headers.Authorization[0]?.Split("Bearer ").FirstOrDefault();
                //
                //         return Task.CompletedTask;
                //     }
                // };
            });


        // builder.Services.AddAuthorizationBuilder()
        //     .AddPolicy(nameof(EnumAuthPolicies.User), policyBuilder =>
        //     {
        //         policyBuilder.AddAuthenticationSchemes("Bearer");
        //         policyBuilder.RequireAuthenticatedUser();
        //         policyBuilder.RequireRole("SuperAdmin", "User");
        //     })
        //     .AddPolicy(nameof(EnumAuthPolicies.SuperAdmin), policyBuilder =>
        //     {
        //         policyBuilder.AddAuthenticationSchemes("Bearer");
        //         policyBuilder.RequireAuthenticatedUser();
        //         policyBuilder.RequireRole("SuperAdmin");
        //     });


        return builder;
    }

    private static WebApplicationBuilder ConfigureEmail(this WebApplicationBuilder builder)
    {
        builder.Services.AddOptions<EmailOption>()
            .BindConfiguration("Email")
            .ValidateOnStart();

        return builder;
    }

    private static WebApplicationBuilder AddRpcServices(this WebApplicationBuilder builder)
    {
        return builder;
    }

    private static WebApplicationBuilder ConfigureHealthCheck(this WebApplicationBuilder builder)
    {
        builder.Services.AddHealthChecks();
        return builder;
    }

    private static WebApplicationBuilder AddApplicationErrors(this WebApplicationBuilder builder)
    {
        //var botConfig = builder.Configuration.GetSection("TelegramBot")
        //     .Get<TelegramBotConfig>();

        //var dbConfig = builder.Configuration.GetSection("ConnectionStrings")
        //     .Get<DatabaseConfig>();

        //builder.AddAppError(dbConfig, botConfig);
        return builder;
    }

    private static WebApplicationBuilder AddServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddMemoryCache();
        builder.Services.AddHttpContextAccessor();
        return builder;
    }

    public static WebApplication UseStaticFiles(this WebApplication app)
    {
        var cacheMaxAgeOneWeek = (60 * 60 * 24 * 7).ToString(); // 7 days = 1 week

        app.UseStaticFiles(new StaticFileOptions()
        {
            HttpsCompression = HttpsCompressionMode.Compress,
            ServeUnknownFileTypes = true,
            OnPrepareResponse = context =>
            {
                context.Context.Response.Headers.Append(
                    "Cache-Control",
                    $"public, max-age={cacheMaxAgeOneWeek}");
            }
        });

        return app;
    }
}