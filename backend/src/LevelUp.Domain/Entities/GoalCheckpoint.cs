using LevelUp.Domain.Common;

namespace LevelUp.Domain.Entities;

/// <summary>
/// A logged progress update for a goal. Each checkpoint is its own row, forming the goal's history.
/// </summary>
public class GoalCheckpoint : AuditableEntity
{
    public const int NoteMaxLength = 1000;

    public int GoalId { get; set; }
    public Goal Goal { get; set; } = null!;

    public DateOnly CheckpointDate { get; set; }
    public int PercentComplete { get; set; }
    public string? Note { get; set; }
}
