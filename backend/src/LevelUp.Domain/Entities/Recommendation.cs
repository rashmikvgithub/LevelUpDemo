using LevelUp.Domain.Common;
using LevelUp.Domain.Enums;

namespace LevelUp.Domain.Entities;

/// <summary>
/// A stored suggestion shown when the user asks for help.
/// </summary>
public class Recommendation : AuditableEntity
{
    public const int TextMaxLength = 500;

    public string Text { get; set; } = string.Empty;
    public RecommendationCategory Category { get; set; } = RecommendationCategory.General;
    public bool IsActive { get; set; } = true;

    public ICollection<RecommendationHistory> History { get; set; } = new List<RecommendationHistory>();
}
