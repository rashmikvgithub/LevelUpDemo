using LevelUp.Application.Activities;
using LevelUp.Application.Common.Interfaces;
using LevelUp.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LevelUp.Infrastructure.Persistence.Repositories;

public sealed class ActivityRepository(LevelUpDbContext context) : Repository<Activity>(context), IActivityRepository
{
    public async Task<IReadOnlyList<Activity>> SearchAsync(ActivityQuery query, CancellationToken cancellationToken = default)
    {
        var q = Set.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            q = q.Where(a => a.Title.Contains(term) || (a.Description != null && a.Description.Contains(term)));
        }

        if (query.Status.HasValue) q = q.Where(a => a.Status == query.Status);
        if (query.AddedFrom.HasValue) q = q.Where(a => a.StartDate >= query.AddedFrom);
        if (query.AddedTo.HasValue) q = q.Where(a => a.StartDate <= query.AddedTo);
        if (query.RetiredFrom.HasValue) q = q.Where(a => a.RetiredDate >= query.RetiredFrom);
        if (query.RetiredTo.HasValue) q = q.Where(a => a.RetiredDate <= query.RetiredTo);

        return await q.OrderBy(a => a.Status).ThenBy(a => a.Title).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Activity>> GetTrackedBetweenAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken = default) =>
        await Set.AsNoTracking()
            .Where(a => a.StartDate <= to && (a.RetiredDate == null || a.RetiredDate >= from))
            .ToListAsync(cancellationToken);
}

public sealed class ActivityScoreRepository(LevelUpDbContext context) : Repository<ActivityScore>(context), IActivityScoreRepository
{
    public Task<ActivityScore?> GetForDateAsync(int activityId, DateOnly date, CancellationToken cancellationToken = default) =>
        Set.FirstOrDefaultAsync(s => s.ActivityId == activityId && s.EvaluationDate == date, cancellationToken);

    public Task<ActivityScore?> GetWithActivityAsync(int scoreId, CancellationToken cancellationToken = default) =>
        Set.Include(s => s.Activity).FirstOrDefaultAsync(s => s.Id == scoreId, cancellationToken);

    public async Task<IReadOnlyList<ActivityScore>> ListForActivityAsync(int activityId, DateOnly? from, DateOnly? to, CancellationToken cancellationToken = default)
    {
        var q = Set.AsNoTracking().Include(s => s.Activity).Where(s => s.ActivityId == activityId);
        if (from.HasValue) q = q.Where(s => s.EvaluationDate >= from);
        if (to.HasValue) q = q.Where(s => s.EvaluationDate <= to);
        return await q.OrderByDescending(s => s.EvaluationDate).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ActivityScore>> ListBetweenAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken = default) =>
        await Set.AsNoTracking()
            .Where(s => s.EvaluationDate >= from && s.EvaluationDate <= to)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ActivityScore>> GetRecentForActivityAsync(int activityId, DateOnly onOrBefore, int count, CancellationToken cancellationToken = default) =>
        await Set.AsNoTracking()
            .Where(s => s.ActivityId == activityId && s.EvaluationDate <= onOrBefore)
            .OrderByDescending(s => s.EvaluationDate)
            .Take(count)
            .ToListAsync(cancellationToken);
}
