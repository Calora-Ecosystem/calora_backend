using BRB.Core.EF.DbContext;
using Core.Entities;
using Core.Entities.Auth;
using Core.Entities.FoodEntites;
using Core.Entities.Notification;
using Core.Entities.Refs;
using Microsoft.EntityFrameworkCore;

namespace Core.Brokers.DbContext;

public class AppDbContext : DefaultConfiguredDbContext
{
    public DbSet<User> Users { get; set; }
    public DbSet<Device> Devices { get; set; }
    public DbSet<SignLog> SignLogs { get; set; }
    public DbSet<UserExtra> UserExtras { get; set; }
    public DbSet<UserNormGeneral> UserNormsGeneral { get; set; }
    public DbSet<UserNormByMenu> UserNormByMenus { get; set; }
    public DbSet<UserDaily> UserDailies { get; set; }
    public DbSet<Reminder> Reminders { get; set; }

    #region Food

    public DbSet<DailyMenu> DailyMenus { get; set; }
    public DbSet<Food> Foods { get; set; }
    public DbSet<FoodCategory> FoodCategories { get; set; }
    public DbSet<FoodMetrics> FoodMetrics { get; set; }

    #endregion

    #region References

    public DbSet<Purpose> Purposes { get; set; }
    public DbSet<Moment> Moments { get; set; }

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

        modelBuilder.Entity<UserExtra>().HasMany(x => x.Purposes).WithMany();
        modelBuilder.Entity<UserExtra>().HasMany(x => x.FavouriteFoods).WithMany();
    }
}