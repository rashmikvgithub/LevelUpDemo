using FluentValidation;
using LevelUp.Application.Common.Exceptions;
using LevelUp.Application.Common.Interfaces;
using LevelUp.Domain.Entities;
using LevelUp.Domain.Enums;

namespace LevelUp.Application.Recommendations;

public interface IRecommendationService
{
    Task<IReadOnlyList<RecommendationDto>> ListAsync(RecommendationCategory? category, bool includeInactive, CancellationToken cancellationToken = default);
    Task<RecommendationDto> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<RecommendationDto> CreateAsync(SaveRecommendationRequest request, CancellationToken cancellationToken = default);
    Task<RecommendationDto> UpdateAsync(int id, SaveRecommendationRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Picks a recommendation for the given context and records it in history.</summary>
    Task<ShownRecommendationDto> GetSuggestionAsync(RecommendationRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RecommendationHistoryDto>> GetHistoryAsync(int? activityId, int? goalId, int take, CancellationToken cancellationToken = default);
}

public sealed class RecommendationService(
    IRecommendationRepository recommendations,
    IRecommendationHistoryRepository history,
    IActivityRepository activities,
    IActivityScoreRepository activityScores,
    IGoalRepository goals,
    IRecommendationProvider provider,
    IUnitOfWork unitOfWork,
    IDateTimeProvider clock,
    IValidator<SaveRecommendationRequest> saveValidator,
    IValidator<RecommendationRequest> requestValidator) : IRecommendationService
{
    public const int MaxHistoryTake = 200;

    public async Task<IReadOnlyList<RecommendationDto>> ListAsync(RecommendationCategory? category, bool includeInactive, CancellationToken cancellationToken = default)
    {
        var result = await recommendations.ListAsync(category, includeInactive, cancellationToken);
        return result.Select(ToDto).ToList();
    }

    public async Task<RecommendationDto> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        ToDto(await GetOrThrowAsync(id, cancellationToken));

    public async Task<RecommendationDto> CreateAsync(SaveRecommendationRequest request, CancellationToken cancellationToken = default)
    {
        await saveValidator.ValidateAndThrowAsync(request, cancellationToken);
        var entity = new Recommendation { Text = request.Text.Trim(), Category = request.Category, IsActive = request.IsActive };
        await recommendations.AddAsync(entity, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ToDto(entity);
    }

    public async Task<RecommendationDto> UpdateAsync(int id, SaveRecommendationRequest request, CancellationToken cancellationToken = default)
    {
        await saveValidator.ValidateAndThrowAsync(request, cancellationToken);
        var entity = await GetOrThrowAsync(id, cancellationToken);
        entity.Text = request.Text.Trim();
        entity.Category = request.Category;
        entity.IsActive = request.IsActive;
        recommendations.Update(entity);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ToDto(entity);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await GetOrThrowAsync(id, cancellationToken);
        recommendations.Remove(entity);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<ShownRecommendationDto> GetSuggestionAsync(RecommendationRequest request, CancellationToken cancellationToken = default)
    {
        await requestValidator.ValidateAndThrowAsync(request, cancellationToken);

        Activity? activity = null;
        IReadOnlyList<string> notes = [];
        if (request.ActivityId is { } activityId)
        {
            activity = await activities.GetByIdAsync(activityId, cancellationToken)
                ?? throw new NotFoundException(nameof(Activity), activityId);
            var scores = await activityScores.ListForActivityAsync(activityId, request.From, request.To, cancellationToken);
            notes = scores
                .Where(score => !string.IsNullOrWhiteSpace(score.Notes))
                .Select(score => $"{score.EvaluationDate:yyyy-MM-dd} (score {score.Score}/5): {score.Notes!.Trim()}")
                .ToList();
        }
        if (request.GoalId is { } goalId && !await goals.ExistsAsync(goalId, cancellationToken))
            throw new NotFoundException(nameof(Goal), goalId);

        var context = new RecommendationContext(request, activity?.Title, notes);
        var recommendation = await provider.GetRecommendationAsync(context, cancellationToken)
            ?? throw new BusinessRuleException("No active recommendations are available. Add some on the Recommendations page.");

        var entry = new RecommendationHistory
        {
            RecommendationId = recommendation.Id == 0 ? null : recommendation.Id,
            RecommendationText = recommendation.Text,
            ActivityId = request.ActivityId,
            GoalId = request.GoalId,
            Trigger = request.Trigger,
            ShownDate = clock.UtcNow
        };
        await history.AddAsync(entry, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new ShownRecommendationDto(entry.Id, entry.RecommendationId, recommendation.Text, recommendation.Category, entry.ShownDate);
    }

    public async Task<IReadOnlyList<RecommendationHistoryDto>> GetHistoryAsync(int? activityId, int? goalId, int take, CancellationToken cancellationToken = default)
    {
        take = Math.Clamp(take, 1, MaxHistoryTake);
        var result = await history.ListAsync(activityId, goalId, take, cancellationToken);
        return result
            .Select(h => new RecommendationHistoryDto(
                h.Id, h.RecommendationId, h.RecommendationText, h.ActivityId, h.Activity?.Title,
                h.GoalId, h.Goal?.Title, h.Trigger, h.ShownDate))
            .ToList();
    }

    private async Task<Recommendation> GetOrThrowAsync(int id, CancellationToken cancellationToken) =>
        await recommendations.GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException(nameof(Recommendation), id);

    private static RecommendationDto ToDto(Recommendation r) =>
        new(r.Id, r.Text, r.Category, r.IsActive, r.CreatedDate, r.ModifiedDate);
}
