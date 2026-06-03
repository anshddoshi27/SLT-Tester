using Microsoft.EntityFrameworkCore;
using SltVirtualTest.Api.Data.Entities;

namespace SltVirtualTest.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<UserEntity> Users => Set<UserEntity>();
    public DbSet<TestRunEntity> TestRuns => Set<TestRunEntity>();
    public DbSet<TestRunStepEntity> TestRunSteps => Set<TestRunStepEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserEntity>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Username).IsUnique();
        });

        modelBuilder.Entity<TestRunEntity>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId);
        });

        modelBuilder.Entity<TestRunStepEntity>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasOne(x => x.TestRun).WithMany(x => x.Steps).HasForeignKey(x => x.TestRunId);
        });
    }
}
