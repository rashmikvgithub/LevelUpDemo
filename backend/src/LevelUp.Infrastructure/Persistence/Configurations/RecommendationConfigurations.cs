using LevelUp.Domain.Entities;
using LevelUp.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LevelUp.Infrastructure.Persistence.Configurations;

public sealed class RecommendationConfiguration : IEntityTypeConfiguration<Recommendation>
{
    private static readonly DateTime SeedDate = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public void Configure(EntityTypeBuilder<Recommendation> builder)
    {
        builder.ToTable("Recommendations");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasColumnName("RecommendationId");
        builder.Property(r => r.Text).HasColumnName("RecommendationText").HasMaxLength(Recommendation.TextMaxLength).IsRequired();
        builder.Property(r => r.Category).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(r => r.IsActive).IsRequired();

        builder.HasIndex(r => new { r.Category, r.IsActive });

        builder.ConfigureAudit();
        builder.HasData(Seed());
    }

    private static IEnumerable<Recommendation> Seed()
    {
        (string Text, RecommendationCategory Category)[] items =
        [
            ("Reschedule the activity to a better time of day when you have more energy.", RecommendationCategory.Activity),
            ("Break the activity into smaller tasks you can finish in under 10 minutes.", RecommendationCategory.Activity),
            ("Set a reminder on your phone or calendar for tomorrow.", RecommendationCategory.Activity),
            ("Reduce the task complexity - do a minimal version on busy days.", RecommendationCategory.Activity),
            ("Improve consistency by doing it at the same time and place every day.", RecommendationCategory.Activity),
            ("Stack it onto an existing habit (e.g. right after your morning coffee).", RecommendationCategory.Activity),
            ("Prepare everything you need the night before to remove friction.", RecommendationCategory.Activity),
            ("Break the goal into monthly milestones and focus only on the next one.", RecommendationCategory.Goal),
            ("Schedule a weekly 15-minute review of this goal in your calendar.", RecommendationCategory.Goal),
            ("Share the goal with a friend or mentor for accountability.", RecommendationCategory.Goal),
            ("Identify the single biggest obstacle and plan one action to remove it.", RecommendationCategory.Goal),
            ("Revisit the target date - an achievable deadline beats an abandoned one.", RecommendationCategory.Goal),
            ("Celebrate small wins - progress is progress.", RecommendationCategory.General),
            ("Track it visibly: a streak you can see is a streak you will protect.", RecommendationCategory.General)
        ];

        return items.Select((item, i) => new Recommendation
        {
            Id = i + 1,
            Text = item.Text,
            Category = item.Category,
            IsActive = true,
            CreatedBy = "seed",
            CreatedDate = SeedDate
        });
    }
}

public sealed class RecommendationHistoryConfiguration : IEntityTypeConfiguration<RecommendationHistory>
{
    public void Configure(EntityTypeBuilder<RecommendationHistory> builder)
    {
        builder.ToTable("RecommendationHistory");
        builder.HasKey(h => h.Id);
        builder.Property(h => h.Id).HasColumnName("RecommendationHistoryId");
        builder.Property(h => h.RecommendationText).HasMaxLength(Recommendation.TextMaxLength).IsRequired();
        builder.Property(h => h.Trigger).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(h => h.ShownDate).HasColumnType("datetime2").IsRequired();

        builder.HasOne(h => h.Recommendation)
            .WithMany(r => r.History)
            .HasForeignKey(h => h.RecommendationId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(h => h.Activity)
            .WithMany()
            .HasForeignKey(h => h.ActivityId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(h => h.Goal)
            .WithMany()
            .HasForeignKey(h => h.GoalId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(h => h.ShownDate);

        builder.ConfigureAudit();
    }
}
