using LevelUp.Application.Activities;
using LevelUp.Application.Goals;
using LevelUp.Domain.Entities;
using LevelUp.Domain.Enums;

namespace LevelUp.Application.Common.Interfaces;

public interface IActivityRepository : IRepository<Activity>
{
    Task<IReadOnlyList<Activity>> SearchAsync(ActivityQuery query, CancellationToken cancellationToken = default);

    /// <summary>Activities that were being tracked at any point within the inclusive date range.</summary>
    Task<IReadOnlyList<Activity>> GetTrackedBetweenAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken = default);
}

public interface IActivityScoreRepository : IRepository<ActivityScore>
{
    Task<ActivityScore?> GetForDateAsync(int activityId, DateOnly date, CancellationToken cancellationToken = default);
    Task<ActivityScore?> GetWithActivityAsync(int scoreId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ActivityScore>> ListForActivityAsync(int activityId, DateOnly? from, DateOnly? to, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ActivityScore>> ListBetweenAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ActivityScore>> GetRecentForActivityAsync(int activityId, DateOnly onOrBefore, int count, CancellationToken cancellationToken = default);
}

public interface IGoalRepository : IRepository<Goal>
{
    Task<IReadOnlyList<Goal>> SearchAsync(GoalQuery query, CancellationToken cancellationToken = default);
    Task<Goal?> GetWithCheckpointsAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Goal>> ListWithCheckpointsAsync(CancellationToken cancellationToken = default);
}

public interface IRecommendationRepository : IRepository<Recommendation>
{
    Task<IReadOnlyList<Recommendation>> ListAsync(RecommendationCategory? category, bool includeInactive, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<int>> GetActiveIdsAsync(IReadOnlyCollection<RecommendationCategory> categories, CancellationToken cancellationToken = default);
}

public interface IRecommendationHistoryRepository : IRepository<RecommendationHistory>
{
    Task<IReadOnlyList<RecommendationHistory>> ListAsync(int? activityId, int? goalId, int take, CancellationToken cancellationToken = default);
    Task<int?> GetLastShownRecommendationIdAsync(int? activityId, int? goalId, CancellationToken cancellationToken = default);
}
