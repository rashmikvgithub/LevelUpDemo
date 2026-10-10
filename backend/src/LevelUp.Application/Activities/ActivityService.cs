using FluentValidation;
using LevelUp.Application.Common.Exceptions;
using LevelUp.Application.Common.Interfaces;
using LevelUp.Domain.Entities;
using LevelUp.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace LevelUp.Application.Activities;

public interface IActivityService
{
    Task<IReadOnlyList<ActivityDto>> SearchAsync(ActivityQuery query, CancellationToken cancellationToken = default);
    Task<ActivityDto> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<ActivityDto> CreateAsync(CreateActivityRequest request, CancellationToken cancellationToken = default);
    Task<ActivityDto> UpdateAsync(int id, UpdateActivityRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DailyActivityDto>> GetDailyAsync(DateOnly date, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ActivityScoreDto>> GetScoresAsync(int activityId, DateOnly? from, DateOnly? to, CancellationToken cancellationToken = default);
    Task<SaveActivityScoreResult> SaveScoreAsync(int activityId, SaveActivityScoreRequest request, CancellationToken cancellationToken = default);
    Task DeleteScoreAsync(int activityId, int scoreId, CancellationToken cancellationToken = default);
}

public sealed class ActivityService(
    IActivityRepository activities,
    IActivityScoreRepository scores,
    IUnitOfWork unitOfWork,
    IDateTimeProvider clock,
    IValidator<CreateActivityRequest> createValidator,
    IValidator<UpdateActivityRequest> updateValidator,
    IValidator<ActivityQuery> queryValidator,
    IValidator<SaveActivityScoreRequest> scoreValidator,
    ILogger<ActivityService> logger) : IActivityService
{
    /// <summary>Number of most recent evaluations inspected when deciding whether to offer help.</summary>
    public const int StruggleWindow = 5;

    /// <summary>How many low scores within the window trigger a help suggestion.</summary>
    public const int StruggleThreshold = 3;

    /// <summary>Scores at or below this value count as "low" (0 = skipped).</summary>
    public const int LowScore = 1;

    public async Task<IReadOnlyList<ActivityDto>> SearchAsync(ActivityQuery query, CancellationToken cancellationToken = default)
    {
        await queryValidator.ValidateAndThrowAsync(query, cancellationToken);
        var result = await activities.SearchAsync(query, cancellationToken);
        return result.Select(a => a.ToDto()).ToList();
    }

    public async Task<ActivityDto> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        (await GetActivityOrThrowAsync(id, cancellationToken)).ToDto();

    public async Task<ActivityDto> CreateAsync(CreateActivityRequest request, CancellationToken cancellationToken = default)
    {
        await createValidator.ValidateAndThrowAsync(request, cancellationToken);

        var activity = new Activity
        {
            Title = request.Title.Trim(),
            Description = request.Description?.Trim(),
            StartDate = request.StartDate ?? clock.Today,
            Status = ActivityStatus.Active
        };

        await activities.AddAsync(activity, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Created activity {ActivityId} '{Title}'", activity.Id, activity.Title);
        return activity.ToDto();
    }

    public async Task<ActivityDto> UpdateAsync(int id, UpdateActivityRequest request, CancellationToken cancellationToken = default)
    {
        await updateValidator.ValidateAndThrowAsync(request, cancellationToken);
        var activity = await GetActivityOrThrowAsync(id, cancellationToken);

        activity.Title = request.Title.Trim();
        activity.Description = request.Description?.Trim();
        activity.StartDate = request.StartDate;

        if (request.Status == ActivityStatus.Retired)
            activity.Retire(request.RetiredDate ?? activity.RetiredDate ?? clock.Today);
        else
            activity.Reactivate();

        if (activity.RetiredDate < activity.StartDate)
            throw new BusinessRuleException("Retired date cannot be before the start date.");

        activities.Update(activity);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return activity.ToDto();
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var activity = await GetActivityOrThrowAsync(id, cancellationToken);
        activities.Remove(activity);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Deleted activity {ActivityId}", id);
    }

    public async Task<IReadOnlyList<DailyActivityDto>> GetDailyAsync(DateOnly date, CancellationToken cancellationToken = default)
    {
        var tracked = await activities.GetTrackedBetweenAsync(date, date, cancellationToken);
        var dayScores = (await scores.ListBetweenAsync(date, date, cancellationToken))
            .ToDictionary(s => s.ActivityId);

        return tracked
            .OrderBy(a => a.Title)
            .Select(a =>
            {
                dayScores.TryGetValue(a.Id, out var s);
                return new DailyActivityDto(a.Id, a.Title, a.Description, a.Status, date, s?.Id, s?.Score, s?.Notes);
            })
            .ToList();
    }

    public async Task<IReadOnlyList<ActivityScoreDto>> GetScoresAsync(int activityId, DateOnly? from, DateOnly? to, CancellationToken cancellationToken = default)
    {
        await EnsureActivityExistsAsync(activityId, cancellationToken);
        var result = await scores.ListForActivityAsync(activityId, from, to, cancellationToken);
        return result.Select(s => s.ToDto()).ToList();
    }

    /// <summary>Creates or updates the grade for the given activity and day (upsert).</summary>
    public async Task<SaveActivityScoreResult> SaveScoreAsync(int activityId, SaveActivityScoreRequest request, CancellationToken cancellationToken = default)
    {
        await scoreValidator.ValidateAndThrowAsync(request, cancellationToken);
        var activity = await GetActivityOrThrowAsync(activityId, cancellationToken);

        if (request.EvaluationDate < activity.StartDate)
            throw new BusinessRuleException(
                $"'{activity.Title}' was not tracked before {activity.StartDate:yyyy-MM-dd}. Adjust the activity's start date to grade earlier days.");
        if (activity.RetiredDate is { } retired && request.EvaluationDate > retired)
            throw new BusinessRuleException($"'{activity.Title}' was retired on {retired:yyyy-MM-dd} and cannot be graded after that date.");

        var score = await scores.GetForDateAsync(activityId, request.EvaluationDate, cancellationToken);
        if (score is null)
        {
            score = new ActivityScore { ActivityId = activityId, Activity = activity, EvaluationDate = request.EvaluationDate };
            await scores.AddAsync(score, cancellationToken);
        }
        else
        {
            scores.Update(score);
        }

        score.Score = request.Score;
        score.Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim();
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var (suggestHelp, reason) = await EvaluateStruggleAsync(activity, request, cancellationToken);
        return new SaveActivityScoreResult(score.ToDto(), suggestHelp, reason);
    }

    public async Task DeleteScoreAsync(int activityId, int scoreId, CancellationToken cancellationToken = default)
    {
        var score = await scores.GetByIdAsync(scoreId, cancellationToken);
        if (score is null || score.ActivityId != activityId)
            throw new NotFoundException(nameof(ActivityScore), scoreId);

        scores.Remove(score);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<(bool SuggestHelp, string? Reason)> EvaluateStruggleAsync(
        Activity activity, SaveActivityScoreRequest request, CancellationToken cancellationToken)
    {
        if (request.Score > LowScore)
            return (false, null);

        var recent = await scores.GetRecentForActivityAsync(activity.Id, request.EvaluationDate, StruggleWindow, cancellationToken);
        var lowCount = recent.Count(s => s.Score <= LowScore);
        if (lowCount < StruggleThreshold)
            return (false, null);

        return (true, $"'{activity.Title}' has been skipped or scored low {lowCount} of the last {recent.Count} times.");
    }

    private async Task<Activity> GetActivityOrThrowAsync(int id, CancellationToken cancellationToken) =>
        await activities.GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException(nameof(Activity), id);

    private async Task EnsureActivityExistsAsync(int id, CancellationToken cancellationToken)
    {
        if (!await activities.ExistsAsync(id, cancellationToken))
            throw new NotFoundException(nameof(Activity), id);
    }
}
