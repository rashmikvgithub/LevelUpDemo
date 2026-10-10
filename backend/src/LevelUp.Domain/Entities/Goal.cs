using LevelUp.Domain.Common;
using LevelUp.Domain.Enums;

namespace LevelUp.Domain.Entities;

/// <summary>
/// A long-term goal tracked through progress checkpoints rather than daily grades.
/// </summary>
public class Goal : AuditableEntity
{
    public const int TitleMaxLength = 200;
    public const int DescriptionMaxLength = 1000;

    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateOnly? TargetDate { get; set; }

    /// <summary>
    /// Denormalized copy of the latest checkpoint's percentage, kept in sync by the service layer
    /// so goals can be filtered/sorted by progress efficiently.
    /// </summary>
    public int ProgressPercentage { get; set; }

    public DateOnly? CompletedDate { get; set; }

    public ICollection<GoalCheckpoint> Checkpoints { get; set; } = new List<GoalCheckpoint>();

    public GoalStatus Status => GetStatus(ProgressPercentage);

    public static GoalStatus GetStatus(int progressPercentage) => progressPercentage switch
    {
        <= 0 => GoalStatus.NotStarted,
        >= 100 => GoalStatus.Completed,
        _ => GoalStatus.InProgress
    };

    /// <summary>Re-derives progress from the most recent checkpoint.</summary>
    public void RecalculateProgress()
    {
        var latest = Checkpoints
            .OrderByDescending(c => c.CheckpointDate)
            .ThenByDescending(c => c.CreatedDate)
            .ThenByDescending(c => c.Id)
            .FirstOrDefault();

        ProgressPercentage = latest?.PercentComplete ?? 0;
        CompletedDate = ProgressPercentage >= 100 ? latest!.CheckpointDate : null;
    }
}
