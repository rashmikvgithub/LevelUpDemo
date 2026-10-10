using LevelUp.Domain.Enums;

namespace LevelUp.Application.Activities;

public sealed record ActivityDto(
    int Id,
    string Title,
    string? Description,
    ActivityStatus Status,
    DateOnly StartDate,
    DateOnly? RetiredDate,
    DateTime CreatedDate,
    DateTime? ModifiedDate);

public sealed record CreateActivityRequest(string Title, string? Description, DateOnly? StartDate);

public sealed record UpdateActivityRequest(
    string Title,
    string? Description,
    DateOnly StartDate,
    ActivityStatus Status,
    DateOnly? RetiredDate);

/// <summary>Search / filter criteria for the activity list.</summary>
public sealed record ActivityQuery
{
    public string? Search { get; init; }
    public ActivityStatus? Status { get; init; }
    public DateOnly? AddedFrom { get; init; }
    public DateOnly? AddedTo { get; init; }
    public DateOnly? RetiredFrom { get; init; }
    public DateOnly? RetiredTo { get; init; }
}

/// <summary>A daily evaluation (grade) of an activity.</summary>
public sealed record ActivityScoreDto(
    int Id,
    int ActivityId,
    string Title,
    string? Description,
    ActivityStatus Status,
    DateOnly EvaluationDate,
    int Score,
    string? Notes,
    DateTime CreatedDate,
    DateTime? ModifiedDate);

public sealed record SaveActivityScoreRequest(DateOnly EvaluationDate, int Score, string? Notes);

/// <summary>
/// Result of saving a grade. <see cref="SuggestHelp"/> is true when the activity has been
/// repeatedly skipped or scored low, so the UI can offer a recommendation.
/// </summary>
public sealed record SaveActivityScoreResult(ActivityScoreDto Score, bool SuggestHelp, string? HelpReason);

/// <summary>An activity row for a specific day, with its grade if one exists.</summary>
public sealed record DailyActivityDto(
    int ActivityId,
    string Title,
    string? Description,
    ActivityStatus Status,
    DateOnly Date,
    int? ScoreId,
    int? Score,
    string? Notes);
