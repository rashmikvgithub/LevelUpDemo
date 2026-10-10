using LevelUp.Domain.Common;

namespace LevelUp.Domain.Entities;

/// <summary>
/// A self-evaluation (0-5) of an activity for a given day. One score per activity per day.
/// </summary>
public class ActivityScore : AuditableEntity
{
    public const int MinScore = 0;
    public const int MaxScore = 5;
    public const int NotesMaxLength = 1000;

    public int ActivityId { get; set; }
    public Activity Activity { get; set; } = null!;

    public DateOnly EvaluationDate { get; set; }

    /// <summary>0 means skipped / failed, 5 is best.</summary>
    public int Score { get; set; }

    public string? Notes { get; set; }
}
