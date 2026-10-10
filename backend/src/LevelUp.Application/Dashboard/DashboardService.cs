using System.Globalization;
using LevelUp.Application.Common.Interfaces;
using LevelUp.Domain.Entities;
using LevelUp.Domain.Enums;

namespace LevelUp.Application.Dashboard;

public interface IDashboardService
{
    Task<DashboardSummaryDto> GetSummaryAsync(DateOnly? date, CancellationToken cancellationToken = default);
    Task<TrendResponseDto> GetTrendsAsync(TrendPeriod period, DateOnly? endDate, CancellationToken cancellationToken = default);
}

public sealed class DashboardService(
    IActivityRepository activities,
    IActivityScoreRepository scores,
    IGoalRepository goals,
    IDateTimeProvider clock) : IDashboardService
{
    public async Task<DashboardSummaryDto> GetSummaryAsync(DateOnly? date, CancellationToken cancellationToken = default)
    {
        var day = date ?? clock.Today;
        var weekStart = StartOfWeek(day);
        var monthStart = new DateOnly(day.Year, day.Month, 1);
        var earliest = weekStart < monthStart ? weekStart : monthStart;

        var tracked = await activities.GetTrackedBetweenAsync(earliest, day, cancellationToken);
        var periodScores = await scores.ListBetweenAsync(earliest, day, cancellationToken);
        var allGoals = await goals.SearchAsync(new Goals.GoalQuery(), cancellationToken);

        return new DashboardSummaryDto(
            day,
            ScoreRange(day, day, tracked, periodScores),
            ScoreRange(weekStart, day, tracked, periodScores),
            ScoreRange(monthStart, day, tracked, periodScores),
            tracked.Count(a => a.IsTrackedOn(day)),
            allGoals.Count(g => g.Status == GoalStatus.InProgress),
            allGoals.Count(g => g.Status == GoalStatus.NotStarted),
            allGoals.Count(g => g.Status == GoalStatus.Completed),
            allGoals.Count == 0 ? 0 : Math.Round((decimal)allGoals.Average(g => g.ProgressPercentage), 1));
    }

    public async Task<TrendResponseDto> GetTrendsAsync(TrendPeriod period, DateOnly? endDate, CancellationToken cancellationToken = default)
    {
        var end = endDate ?? clock.Today;
        var buckets = BuildBuckets(period, end);
        var rangeStart = buckets[0].Start;

        var tracked = await activities.GetTrackedBetweenAsync(rangeStart, end, cancellationToken);
        var rangeScores = await scores.ListBetweenAsync(rangeStart, end, cancellationToken);
        var allGoals = await goals.ListWithCheckpointsAsync(cancellationToken);

        var scoresByActivity = rangeScores.ToLookup(s => s.ActivityId);
        var activitySeries = tracked
            .OrderBy(a => a.Title)
            .Select(a => new TrendSeriesDto(
                a.Id,
                a.Title,
                buckets.Select(b => (decimal)scoresByActivity[a.Id]
                    .Where(s => s.EvaluationDate >= b.Start && s.EvaluationDate <= b.End)
                    .Sum(s => s.Score)).ToList()))
            .ToList();

        var goalSeries = allGoals
            .Where(g => DateOnly.FromDateTime(g.CreatedDate) <= end)
            .OrderBy(g => g.Title)
            .Select(g => new TrendSeriesDto(
                g.Id,
                g.Title,
                buckets.Select(b => (decimal)ProgressAsOf(g, b.End)).ToList()))
            .ToList();

        return new TrendResponseDto(
            period,
            buckets.Select(b => b.Label).ToList(),
            buckets.Select(b => b.Start).ToList(),
            new TrendSeriesDto(null, "All activities", SumSeries(activitySeries, buckets.Count)),
            activitySeries,
            new TrendSeriesDto(null, "All goals", SumSeries(goalSeries, buckets.Count)),
            goalSeries);
    }

    internal static ScoreSummaryDto ScoreRange(
        DateOnly from, DateOnly to, IReadOnlyCollection<Activity> tracked, IReadOnlyCollection<ActivityScore> allScores)
    {
        var possible = 0;
        for (var d = from; d <= to; d = d.AddDays(1))
            possible += tracked.Count(a => a.IsTrackedOn(d)) * ActivityScore.MaxScore;

        var trackedIds = tracked.Select(a => a.Id).ToHashSet();
        var inRange = allScores
            .Where(s => s.EvaluationDate >= from && s.EvaluationDate <= to && trackedIds.Contains(s.ActivityId))
            .ToList();

        var points = inRange.Sum(s => s.Score);
        var percentage = possible == 0 ? 0 : Math.Round(points * 100m / possible, 1);
        decimal? average = inRange.Count == 0 ? null : Math.Round((decimal)inRange.Average(s => s.Score), 2);

        return new ScoreSummaryDto(from, to, points, possible, percentage, average, inRange.Count);
    }

    internal static int ProgressAsOf(Goal goal, DateOnly date) =>
        goal.Checkpoints
            .Where(c => c.CheckpointDate <= date)
            .OrderByDescending(c => c.CheckpointDate)
            .ThenByDescending(c => c.CreatedDate)
            .ThenByDescending(c => c.Id)
            .Select(c => c.PercentComplete)
            .FirstOrDefault();

    internal static DateOnly StartOfWeek(DateOnly date) =>
        date.AddDays(-(((int)date.DayOfWeek + 6) % 7)); // Monday-based weeks

    internal static IReadOnlyList<(DateOnly Start, DateOnly End, string Label)> BuildBuckets(TrendPeriod period, DateOnly end)
    {
        var culture = CultureInfo.InvariantCulture;
        var buckets = new List<(DateOnly, DateOnly, string)>();

        switch (period)
        {
            case TrendPeriod.Daily:
                for (var i = 29; i >= 0; i--)
                {
                    var d = end.AddDays(-i);
                    buckets.Add((d, d, d.ToString("MMM d", culture)));
                }
                break;

            case TrendPeriod.Weekly:
                var currentWeek = StartOfWeek(end);
                for (var i = 11; i >= 0; i--)
                {
                    var s = currentWeek.AddDays(-7 * i);
                    buckets.Add((s, Min(s.AddDays(6), end), s.ToString("MMM d", culture)));
                }
                break;

            case TrendPeriod.Monthly:
                var currentMonth = new DateOnly(end.Year, end.Month, 1);
                for (var i = 11; i >= 0; i--)
                {
                    var s = currentMonth.AddMonths(-i);
                    buckets.Add((s, Min(s.AddMonths(1).AddDays(-1), end), s.ToString("MMM yyyy", culture)));
                }
                break;

            case TrendPeriod.Annual:
                for (var i = 4; i >= 0; i--)
                {
                    var s = new DateOnly(end.Year - i, 1, 1);
                    buckets.Add((s, Min(new DateOnly(s.Year, 12, 31), end), s.Year.ToString(culture)));
                }
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(period), period, "Unsupported trend period.");
        }

        return buckets;
    }

    private static DateOnly Min(DateOnly a, DateOnly b) => a < b ? a : b;

    private static IReadOnlyList<decimal> SumSeries(IReadOnlyCollection<TrendSeriesDto> series, int length) =>
        Enumerable.Range(0, length).Select(i => series.Sum(s => s.Values[i])).ToList();
}
