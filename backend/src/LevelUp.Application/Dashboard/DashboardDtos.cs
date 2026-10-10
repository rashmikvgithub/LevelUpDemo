namespace LevelUp.Application.Dashboard;

public enum TrendPeriod
{
    Daily = 0,
    Weekly = 1,
    Monthly = 2,
    Annual = 3
}

/// <summary>
/// Aggregated self-evaluation score for a date range.
/// <see cref="PossiblePoints"/> = (activities tracked per day x 5) summed over the range.
/// </summary>
public sealed record ScoreSummaryDto(
    DateOnly From,
    DateOnly To,
    int Points,
    int PossiblePoints,
    decimal Percentage,
    decimal? AverageScore,
    int EvaluationCount);

public sealed record DashboardSummaryDto(
    DateOnly Date,
    ScoreSummaryDto Today,
    ScoreSummaryDto Week,
    ScoreSummaryDto Month,
    int ActiveActivities,
    int ActiveGoals,
    int NotStartedGoals,
    int CompletedGoals,
    decimal AverageGoalProgress);

public sealed record TrendSeriesDto(int? Id, string Name, IReadOnlyList<decimal> Values);

public sealed record TrendResponseDto(
    TrendPeriod Period,
    IReadOnlyList<string> Labels,
    IReadOnlyList<DateOnly> BucketStarts,
    TrendSeriesDto ActivitiesTotal,
    IReadOnlyList<TrendSeriesDto> Activities,
    TrendSeriesDto GoalsTotal,
    IReadOnlyList<TrendSeriesDto> Goals);
