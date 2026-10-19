using Microsoft.EntityFrameworkCore;
using TrAInr.Api.Models;

namespace TrAInr.Api.Data;

public class TrainrDbContext(DbContextOptions<TrainrDbContext> options) : DbContext(options)
{
    public DbSet<UserProfile> Profiles => Set<UserProfile>();
    public DbSet<Workout> Workouts => Set<Workout>();
    public DbSet<Exercise> Exercises => Set<Exercise>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserProfile>(e =>
        {
            e.Property(p => p.Name).HasMaxLength(100);
            e.Property(p => p.Sex).HasMaxLength(20);
            e.Property(p => p.Goal).HasMaxLength(50);
            e.Property(p => p.Experience).HasMaxLength(50);
            e.Property(p => p.DietaryPreferences).HasMaxLength(500);
            // En standardprofil tills appen får inloggning och flera användare.
            e.HasData(new UserProfile { Id = 1 });
        });

        modelBuilder.Entity<Workout>(e =>
        {
            e.Property(w => w.Name).HasMaxLength(100);
            e.Property(w => w.Notes).HasMaxLength(1000);
            e.HasIndex(w => w.Date);
            e.HasMany(w => w.Exercises).WithOne().HasForeignKey(x => x.WorkoutId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Exercise>(e => e.Property(x => x.Name).HasMaxLength(100));
    }
}
