using LevelUp.Application.Common.Interfaces;
using LevelUp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace LevelUp.Infrastructure.Persistence;

public sealed class LevelUpDbContext(DbContextOptions<LevelUpDbContext> options) : DbContext(options), IUnitOfWork
{
    public DbSet<Activity> Activities => Set<Activity>();
    public DbSet<ActivityScore> ActivityScores => Set<ActivityScore>();
    public DbSet<Goal> Goals => Set<Goal>();
    public DbSet<GoalCheckpoint> GoalCheckpoints => Set<GoalCheckpoint>();
    public DbSet<Recommendation> Recommendations => Set<Recommendation>();
    public DbSet<RecommendationHistory> RecommendationHistory => Set<RecommendationHistory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LevelUpDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }

    /// <summary>All DateTime values are stored as UTC; mark them as such when read so they serialize with a 'Z'.</summary>
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();
        configurationBuilder.Properties<DateTime?>().HaveConversion<NullableUtcDateTimeConverter>();
    }

    private sealed class UtcDateTimeConverter() : ValueConverter<DateTime, DateTime>(
        v => v.Kind == DateTimeKind.Utc ? v : v.ToUniversalTime(),
        v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

    private sealed class NullableUtcDateTimeConverter() : ValueConverter<DateTime?, DateTime?>(
        v => v.HasValue ? (v.Value.Kind == DateTimeKind.Utc ? v : v.Value.ToUniversalTime()) : v,
        v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v);
}
