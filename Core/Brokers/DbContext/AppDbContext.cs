using BRB.Core.EF.DbContext;
using Core.Entities;
using Core.Entities.Auth;
using Core.Entities.Logging;
using Core.Entities.Billing;
using Core.Entities.Billing.Enum;
using Core.Entities.Billing.Payme;
using Core.Entities.Course;
using Core.Entities.Crm;
using Core.Entities.FoodEntites;
using Core.Entities.Notification;
using Core.Entities.Refs;
using Core.Services.Course.Workout.Contracts;
using Microsoft.EntityFrameworkCore;
using Version = Core.Entities.Refs.Version;

namespace Core.Brokers.DbContext;

public class AppDbContext : DefaultConfiguredDbContext
{
    #region User

    public DbSet<User> Users { get; set; }
    public DbSet<Device> Devices { get; set; }
    public DbSet<SignLog> SignLogs { get; set; }
    public DbSet<UserExtra> UserExtras { get; set; }
    public DbSet<UserNormGeneral> UserNorms { get; set; }
    public DbSet<UserDaily> UserDailies { get; set; }
    public DbSet<UserNormByMenu> UserNormByMenus { get; set; }

    #endregion

    #region Food

    public DbSet<DailyMenu> DailyMenus { get; set; }
    public DbSet<Food> Foods { get; set; }
    public DbSet<FoodCategory> FoodCategories { get; set; }
    public DbSet<FoodMetrics> FoodMetrics { get; set; }

    #endregion

    #region Logging

    public DbSet<EventLog> EventLogs { get; set; }

    #endregion

    #region Course

    public DbSet<Course> Courses { get; set; }
    public DbSet<CourseItemState> CourseItemStates { get; set; }
    public DbSet<Exercise> Exercises { get; set; }
    public DbSet<ExerciseMetric> ExerciseMetrics { get; set; }
    public DbSet<Lesson> Lessons { get; set; }
    public DbSet<Workout> Workouts { get; set; }
    public DbSet<UserStepStat> UserStepStats { get; set; }
    public DbSet<Computation> Computations { get; set; }
    public DbSet<WorkoutComputationIndex> WorkoutComputationIndices { get; set; }

    #endregion

    #region References

    public DbSet<Purpose> Purposes { get; set; }
    public DbSet<Version> Versions { get; set; }

    #endregion

    #region Notifications

    public DbSet<Notification> Notifications { get; set; }
    public DbSet<PushNotification> PushNotifications { get; set; }
    public DbSet<Reminder> Reminders { get; set; }
    public DbSet<ReminderMessage> ReminderMessages { get; set; }

    #endregion

    #region Billing

    public DbSet<Order> Orders { get; set; }
    public DbSet<SubscriptionOrder> SubscriptionOrders { get; set; }
    public DbSet<PlanExtra> PlanExtras { get; set; }
    public DbSet<PlanFeature> PlanFeatures { get; set; }
    public DbSet<Subscription> Subscriptions { get; set; }
    public DbSet<ClickTransaction> ClickTransactions { get; set; }
    public DbSet<PaymeTransaction> PaymeTransactions { get; set; }
    public DbSet<Coupon> Coupons { get; set; }
    public DbSet<CouponUsage> CouponUsages { get; set; }

    #endregion

    #region CRM

    public DbSet<Lead> Leads { get; set; }
    public DbSet<Note> Notes { get; set; }
    public DbSet<LeadActivity> LeadActivities { get; set; }
    public DbSet<FollowUp> FollowUps { get; set; }

    #endregion

    public AppDbContext()
    {
    }

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<UserExtra>().HasMany(x => x.FavouriteFoods).WithMany();

        modelBuilder.Entity<UserStepStat>()
            .HasNoKey();

        modelBuilder.Ignore<UserNormGeneral>();

        modelBuilder
            .Entity<UserNormGeneral>()
            .UseTpcMappingStrategy()
            .ToTable("user_norms")
            .HasNoDiscriminator();

        modelBuilder
            .Entity<UserDaily>()
            .ToTable("user_dailies")
            .HasNoDiscriminator();

        modelBuilder
            .Entity<User>()
            .HasQueryFilter(x => !x.IsDeleted);
    }
}