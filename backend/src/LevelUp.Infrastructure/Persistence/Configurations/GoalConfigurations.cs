using LevelUp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LevelUp.Infrastructure.Persistence.Configurations;

public sealed class GoalConfiguration : IEntityTypeConfiguration<Goal>
{
    public void Configure(EntityTypeBuilder<Goal> builder)
    {
        builder.ToTable("Goal", t =>
            t.HasCheckConstraint("CK_Goal_ProgressPercentage", "[ProgressPercentage] BETWEEN 0 AND 100"));
        builder.HasKey(g => g.Id);
        builder.Property(g => g.Id).HasColumnName("GoalId");
        builder.Property(g => g.Title).HasMaxLength(Goal.TitleMaxLength).IsRequired();
        builder.Property(g => g.Description).HasMaxLength(Goal.DescriptionMaxLength);
        builder.Property(g => g.ProgressPercentage).IsRequired();
        builder.Ignore(g => g.Status);

        builder.HasIndex(g => g.ProgressPercentage);

        builder.HasMany(g => g.Checkpoints)
            .WithOne(c => c.Goal)
            .HasForeignKey(c => c.GoalId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.ConfigureAudit();
    }
}

public sealed class GoalCheckpointConfiguration : IEntityTypeConfiguration<GoalCheckpoint>
{
    public void Configure(EntityTypeBuilder<GoalCheckpoint> builder)
    {
        builder.ToTable("GoalCheckpoints", t =>
            t.HasCheckConstraint("CK_GoalCheckpoints_PercentComplete", "[PercentComplete] BETWEEN 0 AND 100"));
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasColumnName("GoalCheckpointId");
        builder.Property(c => c.Note).HasMaxLength(GoalCheckpoint.NoteMaxLength);
        builder.Property(c => c.CheckpointDate).IsRequired();

        builder.HasIndex(c => new { c.GoalId, c.CheckpointDate });

        builder.ConfigureAudit();
    }
}
