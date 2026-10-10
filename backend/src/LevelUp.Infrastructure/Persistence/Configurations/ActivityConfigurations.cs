using LevelUp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LevelUp.Infrastructure.Persistence.Configurations;

public sealed class ActivityConfiguration : IEntityTypeConfiguration<Activity>
{
    public void Configure(EntityTypeBuilder<Activity> builder)
    {
        builder.ToTable("Activity", t =>
            t.HasCheckConstraint("CK_Activity_RetiredDate", "[RetiredDate] IS NULL OR [RetiredDate] >= [StartDate]"));
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).HasColumnName("ActivityId");
        builder.Property(a => a.Title).HasMaxLength(Activity.TitleMaxLength).IsRequired();
        builder.Property(a => a.Description).HasMaxLength(Activity.DescriptionMaxLength);
        builder.Property(a => a.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(a => a.StartDate).IsRequired();

        builder.HasIndex(a => a.Status);
        builder.HasIndex(a => a.Title);

        builder.HasMany(a => a.Scores)
            .WithOne(s => s.Activity)
            .HasForeignKey(s => s.ActivityId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.ConfigureAudit();
    }
}

public sealed class ActivityScoreConfiguration : IEntityTypeConfiguration<ActivityScore>
{
    public void Configure(EntityTypeBuilder<ActivityScore> builder)
    {
        builder.ToTable("ActivityScores", t =>
            t.HasCheckConstraint("CK_ActivityScores_Score", $"[Score] BETWEEN {ActivityScore.MinScore} AND {ActivityScore.MaxScore}"));
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).HasColumnName("ActivityScoreId");
        builder.Property(s => s.EvaluationDate).IsRequired();
        builder.Property(s => s.Score).IsRequired();
        builder.Property(s => s.Notes).HasMaxLength(ActivityScore.NotesMaxLength);

        // One grade per activity per day.
        builder.HasIndex(s => new { s.ActivityId, s.EvaluationDate }).IsUnique();
        builder.HasIndex(s => s.EvaluationDate);

        builder.ConfigureAudit();
    }
}
