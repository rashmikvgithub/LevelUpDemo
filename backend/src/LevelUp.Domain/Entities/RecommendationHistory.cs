using LevelUp.Domain.Common;
using LevelUp.Domain.Enums;

namespace LevelUp.Domain.Entities;

/// <summary>
/// Log of every recommendation shown to the user and the context it was shown in.
/// </summary>
public class RecommendationHistory : AuditableEntity
{
    public int? RecommendationId { get; set; }
    public Recommendation? Recommendation { get; set; }

    /// <summary>Snapshot of the text at the time it was shown (survives later edits).</summary>
    public string RecommendationText { get; set; } = string.Empty;

    public int? ActivityId { get; set; }
    public Activity? Activity { get; set; }

    public int? GoalId { get; set; }
    public Goal? Goal { get; set; }

    public RecommendationTrigger Trigger { get; set; } = RecommendationTrigger.Manual;
    public DateTime ShownDate { get; set; }
}
