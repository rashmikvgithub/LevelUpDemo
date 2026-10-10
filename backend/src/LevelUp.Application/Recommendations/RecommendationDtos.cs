using LevelUp.Domain.Enums;

namespace LevelUp.Application.Recommendations;

public sealed record RecommendationDto(
    int Id,
    string Text,
    RecommendationCategory Category,
    bool IsActive,
    DateTime CreatedDate,
    DateTime? ModifiedDate);

public sealed record SaveRecommendationRequest(string Text, RecommendationCategory Category, bool IsActive = true);

/// <summary>Context for a "Help Me!" request.</summary>
public sealed record RecommendationRequest
{
    public RecommendationCategory Category { get; init; } = RecommendationCategory.Activity;
    public int? ActivityId { get; init; }
    public int? GoalId { get; init; }
    public RecommendationTrigger Trigger { get; init; } = RecommendationTrigger.Manual;
}

/// <summary>A recommendation that was just shown to the user (and logged to history).</summary>
public sealed record ShownRecommendationDto(
    int HistoryId,
    int RecommendationId,
    string Text,
    RecommendationCategory Category,
    DateTime ShownDate);

public sealed record RecommendationHistoryDto(
    int Id,
    int? RecommendationId,
    string Text,
    int? ActivityId,
    string? ActivityTitle,
    int? GoalId,
    string? GoalTitle,
    RecommendationTrigger Trigger,
    DateTime ShownDate);
