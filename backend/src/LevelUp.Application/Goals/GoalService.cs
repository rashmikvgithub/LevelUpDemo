using FluentValidation;
using LevelUp.Application.Common.Exceptions;
using LevelUp.Application.Common.Interfaces;
using LevelUp.Domain.Entities;
using LevelUp.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace LevelUp.Application.Goals;

public interface IGoalService
{
    Task<IReadOnlyList<GoalDto>> SearchAsync(GoalQuery query, CancellationToken cancellationToken = default);
    Task<GoalDto> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<GoalDto> CreateAsync(CreateGoalRequest request, CancellationToken cancellationToken = default);
    Task<GoalDto> UpdateAsync(int id, UpdateGoalRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<GoalDto> CompleteAsync(int id, CompleteGoalRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<GoalCheckpointDto>> GetCheckpointsAsync(int goalId, CancellationToken cancellationToken = default);
    Task<GoalCheckpointDto> AddCheckpointAsync(int goalId, CreateGoalCheckpointRequest request, CancellationToken cancellationToken = default);
    Task DeleteCheckpointAsync(int goalId, int checkpointId, CancellationToken cancellationToken = default);
}

public sealed class GoalService(
    IGoalRepository goals,
    IUnitOfWork unitOfWork,
    IDateTimeProvider clock,
    IValidator<CreateGoalRequest> createValidator,
    IValidator<UpdateGoalRequest> updateValidator,
    IValidator<CreateGoalCheckpointRequest> checkpointValidator,
    IValidator<CompleteGoalRequest> completeValidator,
    IValidator<GoalQuery> queryValidator,
    ILogger<GoalService> logger) : IGoalService
{
    public async Task<IReadOnlyList<GoalDto>> SearchAsync(GoalQuery query, CancellationToken cancellationToken = default)
    {
        await queryValidator.ValidateAndThrowAsync(query, cancellationToken);
        var result = await goals.SearchAsync(query, cancellationToken);
        return result.Select(g => g.ToDto()).ToList();
    }

    public async Task<GoalDto> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        (await GetGoalOrThrowAsync(id, cancellationToken)).ToDto(includeCheckpoints: true);

    public async Task<GoalDto> CreateAsync(CreateGoalRequest request, CancellationToken cancellationToken = default)
    {
        await createValidator.ValidateAndThrowAsync(request, cancellationToken);
        var goal = new Goal
        {
            Title = request.Title.Trim(),
            Description = request.Description?.Trim(),
            TargetDate = request.TargetDate
        };

        await goals.AddAsync(goal, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Created goal {GoalId} '{Title}'", goal.Id, goal.Title);
        return goal.ToDto();
    }

    public async Task<GoalDto> UpdateAsync(int id, UpdateGoalRequest request, CancellationToken cancellationToken = default)
    {
        await updateValidator.ValidateAndThrowAsync(request, cancellationToken);
        var goal = await GetGoalOrThrowAsync(id, cancellationToken);

        goal.Title = request.Title.Trim();
        goal.Description = request.Description?.Trim();
        goal.TargetDate = request.TargetDate;

        goals.Update(goal);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return goal.ToDto();
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var goal = await GetGoalOrThrowAsync(id, cancellationToken);
        goals.Remove(goal);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Deleted goal {GoalId}", id);
    }

    /// <summary>Marks a goal complete by logging a 100% checkpoint.</summary>
    public async Task<GoalDto> CompleteAsync(int id, CompleteGoalRequest request, CancellationToken cancellationToken = default)
    {
        await completeValidator.ValidateAndThrowAsync(request, cancellationToken);
        var goal = await GetGoalOrThrowAsync(id, cancellationToken);
        if (goal.Status == GoalStatus.Completed)
            throw new BusinessRuleException($"Goal '{goal.Title}' is already completed.");

        AppendCheckpoint(goal, 100, string.IsNullOrWhiteSpace(request.Note) ? "Marked as completed" : request.Note, clock.Today);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return goal.ToDto();
    }

    public async Task<IReadOnlyList<GoalCheckpointDto>> GetCheckpointsAsync(int goalId, CancellationToken cancellationToken = default)
    {
        var goal = await GetGoalOrThrowAsync(goalId, cancellationToken);
        return goal.ToDto(includeCheckpoints: true).Checkpoints!;
    }

    public async Task<GoalCheckpointDto> AddCheckpointAsync(int goalId, CreateGoalCheckpointRequest request, CancellationToken cancellationToken = default)
    {
        await checkpointValidator.ValidateAndThrowAsync(request, cancellationToken);
        var goal = await GetGoalOrThrowAsync(goalId, cancellationToken);

        var checkpoint = AppendCheckpoint(goal, request.PercentComplete, request.Note, request.CheckpointDate ?? clock.Today);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return checkpoint.ToDto();
    }

    public async Task DeleteCheckpointAsync(int goalId, int checkpointId, CancellationToken cancellationToken = default)
    {
        var goal = await GetGoalOrThrowAsync(goalId, cancellationToken);
        var checkpoint = goal.Checkpoints.FirstOrDefault(c => c.Id == checkpointId)
            ?? throw new NotFoundException(nameof(GoalCheckpoint), checkpointId);

        goal.Checkpoints.Remove(checkpoint);
        goal.RecalculateProgress();
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private GoalCheckpoint AppendCheckpoint(Goal goal, int percent, string? note, DateOnly date)
    {
        var checkpoint = new GoalCheckpoint
        {
            Goal = goal,
            PercentComplete = percent,
            Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim(),
            CheckpointDate = date,
            // Set before save so RecalculateProgress treats this as the newest entry for its date.
            CreatedDate = clock.UtcNow
        };
        goal.Checkpoints.Add(checkpoint);
        goal.RecalculateProgress();
        return checkpoint;
    }

    private async Task<Goal> GetGoalOrThrowAsync(int id, CancellationToken cancellationToken) =>
        await goals.GetWithCheckpointsAsync(id, cancellationToken) ?? throw new NotFoundException(nameof(Goal), id);
}
