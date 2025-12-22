using BRB.Core.EF.DbContext;
using Core.Entities;
using Core.Entities.Auth;
using Core.Entities.Course;
using Core.Entities.FoodEntites;
using Core.Entities.Notification;
using Core.Entities.Refs;
using Core.Services.Course.Workout.Contracts;
using Microsoft.EntityFrameworkCore;

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

    #region Course

    public DbSet<Course> Courses { get; set; }
    public DbSet<CourseItemState> CourseItemStates { get; set; }
    public DbSet<Exercise> Exercises { get; set; }
    public DbSet<ExerciseMetric> ExerciseMetrics { get; set; }
    public DbSet<Lesson> Lessons { get; set; }
    public DbSet<Workout> Workouts { get; set; }
    public DbSet<UserStepStat> UserStepStats { get; set; }
    public DbSet<Computation> Computations { get; set; }

    #endregion

    #region References

    public DbSet<Purpose> Purposes { get; set; }

    #endregion

    #region Notifications

    public DbSet<Notification> Notifications { get; set; }
    public DbSet<PushNotification> PushNotifications { get; set; }
    public DbSet<Reminder> Reminders { get; set; }

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
    }
}