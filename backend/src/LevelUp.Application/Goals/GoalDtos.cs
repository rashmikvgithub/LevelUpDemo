using LevelUp.Domain.Enums;

namespace LevelUp.Application.Goals;

public sealed record GoalDto(
    int Id,
    string Title,
    string? Description,
    DateOnly? TargetDate,
    int ProgressPercentage,
    GoalStatus Status,
    DateOnly? CompletedDate,
    DateOnly? LastCheckpointDate,
    int CheckpointCount,
    DateTime CreatedDate,
    DateTime? ModifiedDate,
    IReadOnlyList<GoalCheckpointDto>? Checkpoints = null);

public sealed record GoalCheckpointDto(
    int Id,
    int GoalId,
    DateOnly CheckpointDate,
    int PercentComplete,
    string? Note,
    DateTime CreatedDate);

public sealed record CreateGoalRequest(string Title, string? Description, DateOnly? TargetDate);

public sealed record UpdateGoalRequest(string Title, string? Description, DateOnly? TargetDate);

public sealed record CreateGoalCheckpointRequest(int PercentComplete, string? Note, DateOnly? CheckpointDate);

public sealed record CompleteGoalRequest(string? Note);

public sealed record GoalQuery
{
    public string? Search { get; init; }
    public GoalStatus? Status { get; init; }
}
