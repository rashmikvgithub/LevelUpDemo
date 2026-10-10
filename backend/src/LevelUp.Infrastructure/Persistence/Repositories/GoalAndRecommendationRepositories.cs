using LevelUp.Application.Common.Interfaces;
using LevelUp.Application.Goals;
using LevelUp.Domain.Entities;
using LevelUp.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace LevelUp.Infrastructure.Persistence.Repositories;

public sealed class GoalRepository(LevelUpDbContext context) : Repository<Goal>(context), IGoalRepository
{
    public async Task<IReadOnlyList<Goal>> SearchAsync(GoalQuery query, CancellationToken cancellationToken = default)
    {
        var q = Set.AsNoTracking().Include(g => g.Checkpoints).AsSplitQuery().AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            q = q.Where(g => g.Title.Contains(term) || (g.Description != null && g.Description.Contains(term)));
        }

        q = query.Status switch
        {
            GoalStatus.NotStarted => q.Where(g => g.ProgressPercentage <= 0),
            GoalStatus.InProgress => q.Where(g => g.ProgressPercentage > 0 && g.ProgressPercentage < 100),
            GoalStatus.Completed => q.Where(g => g.ProgressPercentage >= 100),
            _ => q
        };

        return await q
            .OrderBy(g => g.ProgressPercentage >= 100)
            .ThenBy(g => g.TargetDate == null)
            .ThenBy(g => g.TargetDate)
            .ThenBy(g => g.Title)
            .ToListAsync(cancellationToken);
    }

    public Task<Goal?> GetWithCheckpointsAsync(int id, CancellationToken cancellationToken = default) =>
        Set.Include(g => g.Checkpoints).FirstOrDefaultAsync(g => g.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Goal>> ListWithCheckpointsAsync(CancellationToken cancellationToken = default) =>
        await Set.AsNoTracking().Include(g => g.Checkpoints).AsSplitQuery().ToListAsync(cancellationToken);
}

public sealed class RecommendationRepository(LevelUpDbContext context) : Repository<Recommendation>(context), IRecommendationRepository
{
    public async Task<IReadOnlyList<Recommendation>> ListAsync(RecommendationCategory? category, bool includeInactive, CancellationToken cancellationToken = default)
    {
        var q = Set.AsNoTracking().AsQueryable();
        if (category.HasValue) q = q.Where(r => r.Category == category);
        if (!includeInactive) q = q.Where(r => r.IsActive);
        return await q.OrderBy(r => r.Category).ThenBy(r => r.Text).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<int>> GetActiveIdsAsync(IReadOnlyCollection<RecommendationCategory> categories, CancellationToken cancellationToken = default) =>
        await Set.AsNoTracking()
            .Where(r => r.IsActive && categories.Contains(r.Category))
            .Select(r => r.Id)
            .ToListAsync(cancellationToken);
}

public sealed class RecommendationHistoryRepository(LevelUpDbContext context)
    : Repository<RecommendationHistory>(context), IRecommendationHistoryRepository
{
    public async Task<IReadOnlyList<RecommendationHistory>> ListAsync(int? activityId, int? goalId, int take, CancellationToken cancellationToken = default)
    {
        var q = Set.AsNoTracking().Include(h => h.Activity).Include(h => h.Goal).AsQueryable();
        if (activityId.HasValue) q = q.Where(h => h.ActivityId == activityId);
        if (goalId.HasValue) q = q.Where(h => h.GoalId == goalId);
        return await q.OrderByDescending(h => h.ShownDate).Take(take).ToListAsync(cancellationToken);
    }

    public Task<int?> GetLastShownRecommendationIdAsync(int? activityId, int? goalId, CancellationToken cancellationToken = default) =>
        Set.AsNoTracking()
            .Where(h => h.ActivityId == activityId && h.GoalId == goalId)
            .OrderByDescending(h => h.ShownDate)
            .Select(h => h.RecommendationId)
            .FirstOrDefaultAsync(cancellationToken);
}
