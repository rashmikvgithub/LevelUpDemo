using LevelUp.Application.Common.Interfaces;
using LevelUp.Domain.Entities;
using LevelUp.Domain.Enums;

namespace LevelUp.Application.Recommendations;

/// <summary>
/// Strategy for choosing a recommendation. The default implementation picks a random stored
/// suggestion; a future implementation can call an AI/LLM without changing the API or UI.
/// </summary>
public interface IRecommendationProvider
{
    Task<Recommendation?> GetRecommendationAsync(RecommendationContext context, CancellationToken cancellationToken = default);
}

public sealed record RecommendationContext(
    RecommendationRequest Request,
    string? ActivityName,
    IReadOnlyList<string> Notes);

public sealed class RandomRecommendationProvider(
    IRecommendationRepository recommendations,
    IRecommendationHistoryRepository history) : IRecommendationProvider
{
    public async Task<Recommendation?> GetRecommendationAsync(RecommendationContext context, CancellationToken cancellationToken = default)
    {
        var request = context.Request;
        RecommendationCategory[] categories = [request.Category, RecommendationCategory.General];
        var ids = await recommendations.GetActiveIdsAsync(categories.Distinct().ToArray(), cancellationToken);
        if (ids.Count == 0)
            return null;

        // Avoid showing the same suggestion twice in a row for the same context when possible.
        var lastShown = await history.GetLastShownRecommendationIdAsync(request.ActivityId, request.GoalId, cancellationToken);
        var candidates = ids.Count > 1 && lastShown.HasValue ? ids.Where(id => id != lastShown).ToList() : ids.ToList();

        var chosenId = candidates[Random.Shared.Next(candidates.Count)];
        return await recommendations.GetByIdAsync(chosenId, cancellationToken);
    }
}
