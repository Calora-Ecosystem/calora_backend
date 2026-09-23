using System.Reflection;
using BRB.Core.EF.Extensions;
using Core.Brokers.Apple;
using Core.Brokers.EmailBroker;
using Core.Brokers.EskizBroker;
using Core.Services.Auth;
using Core.Services.Ai.Contracts;
using Core.Services.Auth.Contracts;
using Core.Services.Coins.Contracts;
using Core.Services.Billing.Click;
using Core.Services.Billing.Payme.Extensions;
using Core.Services.Billing.Rc.Extensions;
using Core.Services.Common;
using Core.Services.Course.Workout;
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

        // Ikkala bo'lim ixtiyoriy — default qiymatlar klass ichida.
        builder.Services
            .AddOptions<AiQuotaConfig>()
            .BindConfiguration("AiQuota");

        builder.Services
            .AddOptions<CoinConfig>()
            .BindConfiguration("Coins");

        builder
            .Services
            .AddAppleClient()
            .AddEskizClient()
            .AddPaymeConfig()
            .AddRcConfig();
        ;

        builder.Services.AddSingleton<MemoryCacheManager>();

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
        if (!app.Environment.IsDevelopment())
        {
            RecurringJob.AddOrUpdate<NotificationService>("enqueue_notifications",
                service => service.EnqueueNotifications(),
                app.Environment.IsProduction() ? "*/10 * * * *" : "* * * * *");

            RecurringJob.AddOrUpdate<ReminderService>("check_reminders",
                service => service.CheckReminders(), $"*/{ReminderService.CheckReminderWindowInMin} * * * *");

            // RecurringJob.AddOrUpdate<ReminderService>("check_meal_reminders",
            //     service => service.CheckMealReminders(), $"*/{ReminderService.CheckReminderWindowInMin} * * * *");
        }

        RecurringJob.AddOrUpdate<WorkoutService>("index_workout_computations",
            service => service.IndexWorkoutComputations(), "0 0 31 2 *");

        RecurringJob.AddOrUpdate<Core.Services.Crm.LeadService>("crm_escalate_leads",
            service => service.EscalateLeadsAsync(), "*/15 * * * *");

        // Muddati o'tgan coin/referral premiumlarini o'chiradi (to'langan obunalarga tegmaydi).
        RecurringJob.AddOrUpdate<Core.Services.Billing.SubscriptionService>("expire_granted_subscriptions",
            service => service.DeactivateExpiredGrants(), "*/15 * * * *");

        return app;
    }

    public static IServiceCollection AddClickService(this IServiceCollection services)
    {
        services.AddScoped<ClickService>();
        services.AddOptions<ClickConfig>()
            .BindConfiguration("Click")
            .ValidateDataAnnotations()
            .ValidateOnStart();

        return services;
    }
}