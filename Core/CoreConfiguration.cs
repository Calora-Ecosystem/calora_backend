using System.Reflection;
using BRB.Core.EF.Extensions;
using Core.Brokers.EmailBroker;
using Core.Services.Auth;
using Core.Services.Auth.Contracts;
using Core.Services.Notification;
using Hangfire;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;

namespace Core;

public static class CoreConfiguration
{
    public static WebApplicationBuilder AddCore(this WebApplicationBuilder builder)
    {
        builder.Services
            .ConfigureServicesFromTypeAssembly<AuthService>();

        builder.Services.AddEmailClient();

        builder.Services
            .AddOptions<AuthConfig>()
            .BindConfiguration("Auth")
            .ValidateOnStart();

        return builder;
    }

    public static WebApplicationBuilder AddDefaultConfiguredDbContext<T>(this WebApplicationBuilder builder,
        string connectionString = "Default",
        ServiceLifetime? lifetime = null) where T : DbContext
    {
        var dataSourceBuilder =
            new NpgsqlDataSourceBuilder(builder.Configuration.GetConnectionString(connectionString))
                .EnableDynamicJson();

        if (lifetime is null)
            builder.Services.AddDbContextPool<T>(optionsBuilder =>
            {
                optionsBuilder
                    .UseNpgsql(
                        dataSourceBuilder.Build(),
                        options => { options.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery); })
                    .UseSnakeCaseNamingConvention();
            });
        else
            builder.Services.AddDbContext<T>(optionsBuilder =>
            {
                optionsBuilder
                    .UseNpgsql(
                        dataSourceBuilder.Build(),
                        options => { }).UseSnakeCaseNamingConvention();
            }, lifetime.Value, lifetime.Value);

        return builder;
    }

    public static WebApplication AddRecurringJobs(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
            return app;

        RecurringJob.AddOrUpdate<NotificationService>("enqueue_notifications",
            service => service.EnqueueNotifications(), app.Environment.IsProduction() ? "*/10 * * * *" : "* * * * *");

        RecurringJob.AddOrUpdate<ReminderService>("check_reminders",
            service => service.CheckReminders(), "*/30 * * * *");

        return app;
    }
}