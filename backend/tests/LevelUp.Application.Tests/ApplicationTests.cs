using FluentValidation;
using LevelUp.Application.Activities;
using LevelUp.Application.Common.Exceptions;
using LevelUp.Application.Common.Interfaces;
using LevelUp.Application.Dashboard;
using LevelUp.Application.Goals;
using LevelUp.Application.Recommendations;
using LevelUp.Domain.Entities;
using LevelUp.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace LevelUp.Application.Tests;

internal sealed class FixedClock(DateOnly today) : IDateTimeProvider
{
    public DateTime UtcNow => today.ToDateTime(new TimeOnly(12, 0), DateTimeKind.Utc);
    public DateOnly Today => today;
}

public class GoalTests
{
    [Theory]
    [InlineData(0, GoalStatus.NotStarted)]
    [InlineData(1, GoalStatus.InProgress)]
    [InlineData(99, GoalStatus.InProgress)]
    [InlineData(100, GoalStatus.Completed)]
    public void GetStatus_DerivesFromPercentage(int percent, GoalStatus expected) =>
        Assert.Equal(expected, Goal.GetStatus(percent));

    [Fact]
    public void RecalculateProgress_UsesLatestCheckpointByDate()
    {
        var goal = new Goal();
        goal.Checkpoints.Add(new GoalCheckpoint { Id = 1, CheckpointDate = new DateOnly(2026, 1, 10), PercentComplete = 60 });
        goal.Checkpoints.Add(new GoalCheckpoint { Id = 2, CheckpointDate = new DateOnly(2026, 1, 5), PercentComplete = 90 });

        goal.RecalculateProgress();

        Assert.Equal(60, goal.ProgressPercentage);
        Assert.Equal(GoalStatus.InProgress, goal.Status);
        Assert.Null(goal.CompletedDate);
    }

    [Fact]
    public void RecalculateProgress_NoCheckpoints_ResetsToNotStarted()
    {
        var goal = new Goal { ProgressPercentage = 100, CompletedDate = new DateOnly(2026, 1, 1) };
        goal.RecalculateProgress();
        Assert.Equal(0, goal.ProgressPercentage);
        Assert.Null(goal.CompletedDate);
    }
}

public class DashboardCalculationTests
{
    private static readonly DateOnly Day = new(2026, 10, 10); // Saturday

    [Fact]
    public void ScoreRange_ComputesPointsAgainstTrackedActivitiesOnly()
    {
        var a1 = new Activity { Id = 1, StartDate = Day.AddDays(-30) };
        var a2 = new Activity { Id = 2, StartDate = Day };
        ActivityScore[] scores =
        [
            new() { ActivityId = 1, EvaluationDate = Day, Score = 4 },
            new() { ActivityId = 2, EvaluationDate = Day, Score = 2 },
            new() { ActivityId = 1, EvaluationDate = Day.AddDays(-1), Score = 5 }
        ];

        var today = DashboardService.ScoreRange(Day, Day, [a1, a2], scores);
        var twoDays = DashboardService.ScoreRange(Day.AddDays(-1), Day, [a1, a2], scores);

        Assert.Equal(6, today.Points);
        Assert.Equal(10, today.PossiblePoints);
        Assert.Equal(60m, today.Percentage);
        Assert.Equal(3m, today.AverageScore);

        // Activity 2 only started today, so yesterday contributes 5 possible points, not 10.
        Assert.Equal(11, twoDays.Points);
        Assert.Equal(15, twoDays.PossiblePoints);
    }

    [Fact]
    public void ScoreRange_NoActivities_ReturnsZeroWithoutDividingByZero()
    {
        var result = DashboardService.ScoreRange(Day, Day, [], []);
        Assert.Equal(0, result.Percentage);
        Assert.Null(result.AverageScore);
    }

    [Theory]
    [InlineData(TrendPeriod.Daily, 30)]
    [InlineData(TrendPeriod.Weekly, 12)]
    [InlineData(TrendPeriod.Monthly, 12)]
    [InlineData(TrendPeriod.Annual, 5)]
    public void BuildBuckets_ReturnsExpectedCountAndEndsOnDate(TrendPeriod period, int expected)
    {
        var buckets = DashboardService.BuildBuckets(period, Day);
        Assert.Equal(expected, buckets.Count);
        Assert.Equal(Day, buckets[^1].End);
        Assert.All(buckets, b => Assert.True(b.Start <= b.End));
    }

    [Fact]
    public void StartOfWeek_IsMonday() =>
        Assert.Equal(new DateOnly(2026, 10, 5), DashboardService.StartOfWeek(Day));

    [Fact]
    public void ProgressAsOf_IgnoresFutureCheckpoints()
    {
        var goal = new Goal();
        goal.Checkpoints.Add(new GoalCheckpoint { CheckpointDate = Day.AddDays(-5), PercentComplete = 20 });
        goal.Checkpoints.Add(new GoalCheckpoint { CheckpointDate = Day.AddDays(1), PercentComplete = 80 });

        Assert.Equal(20, DashboardService.ProgressAsOf(goal, Day));
        Assert.Equal(0, DashboardService.ProgressAsOf(goal, Day.AddDays(-10)));
    }
}

public class ValidatorTests
{
    private static readonly FixedClock Clock = new(new DateOnly(2026, 10, 10));

    [Theory]
    [InlineData(-1, false)]
    [InlineData(0, true)]
    [InlineData(5, true)]
    [InlineData(6, false)]
    public void SaveScore_ScoreRange(int score, bool valid)
    {
        var result = new SaveActivityScoreRequestValidator(Clock).Validate(new SaveActivityScoreRequest(Clock.Today, score, null));
        Assert.Equal(valid, result.IsValid);
    }

    [Fact]
    public void SaveScore_RejectsFutureDates()
    {
        var result = new SaveActivityScoreRequestValidator(Clock).Validate(new SaveActivityScoreRequest(Clock.Today.AddDays(2), 3, null));
        Assert.False(result.IsValid);
    }

    [Fact]
    public void UpdateActivity_ActiveWithRetiredDate_IsInvalid()
    {
        var request = new UpdateActivityRequest("Read", null, Clock.Today, ActivityStatus.Active, Clock.Today);
        Assert.False(new UpdateActivityRequestValidator().Validate(request).IsValid);
    }

    [Theory]
    [InlineData(-1, false)]
    [InlineData(0, true)]
    [InlineData(100, true)]
    [InlineData(101, false)]
    public void Checkpoint_PercentRange(int percent, bool valid)
    {
        var result = new CreateGoalCheckpointRequestValidator(Clock).Validate(new CreateGoalCheckpointRequest(percent, null, null));
        Assert.Equal(valid, result.IsValid);
    }

    [Fact]
    public void Recommendation_RejectsInvertedDateRange()
    {
        var request = new RecommendationRequest
        {
            ActivityId = 7,
            From = Clock.Today,
            To = Clock.Today.AddDays(-1)
        };

        Assert.False(new RecommendationRequestValidator().Validate(request).IsValid);
    }
}

public class RecommendationServiceTests
{
    private static readonly FixedClock Clock = new(new DateOnly(2026, 10, 10));

    [Fact]
    public async Task GetSuggestion_ProvidesActivityNameAndDateRangeNotesToProvider()
    {
        var recommendations = new Mock<IRecommendationRepository>();
        var history = new Mock<IRecommendationHistoryRepository>();
        var activities = new Mock<IActivityRepository>();
        var scores = new Mock<IActivityScoreRepository>();
        var goals = new Mock<IGoalRepository>();
        var provider = new Mock<IRecommendationProvider>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var from = Clock.Today.AddDays(-7);
        var to = Clock.Today;
        RecommendationContext? capturedContext = null;

        activities.Setup(r => r.GetByIdAsync(7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Activity { Id = 7, Title = "Exercise" });
        scores.Setup(r => r.ListForActivityAsync(7, from, to, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new ActivityScore { EvaluationDate = to, Score = 1, Notes = "  skipped because I was tired  " },
                new ActivityScore { EvaluationDate = to.AddDays(-1), Score = 4, Notes = null }
            ]);
        provider.Setup(p => p.GetRecommendationAsync(It.IsAny<RecommendationContext>(), It.IsAny<CancellationToken>()))
            .Callback<RecommendationContext, CancellationToken>((context, _) => capturedContext = context)
            .ReturnsAsync(new Recommendation { Text = "1. Prepare early.", Category = RecommendationCategory.Activity });

        var service = new RecommendationService(
            recommendations.Object,
            history.Object,
            activities.Object,
            scores.Object,
            goals.Object,
            provider.Object,
            unitOfWork.Object,
            Clock,
            new SaveRecommendationRequestValidator(),
            new RecommendationRequestValidator());
        var request = new RecommendationRequest { ActivityId = 7, From = from, To = to };

        await service.GetSuggestionAsync(request);

        Assert.NotNull(capturedContext);
        Assert.Equal("Exercise", capturedContext.ActivityName);
        Assert.Equal(["2026-10-10 (score 1/5): skipped because I was tired"], capturedContext.Notes);
        scores.Verify(r => r.ListForActivityAsync(7, from, to, It.IsAny<CancellationToken>()), Times.Once);
    }
}

public class ActivityServiceTests
{
    private static readonly FixedClock Clock = new(new DateOnly(2026, 10, 10));

    private readonly Mock<IActivityRepository> _activities = new();
    private readonly Mock<IActivityScoreRepository> _scores = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    private ActivityService CreateService() => new(
        _activities.Object, _scores.Object, _uow.Object, Clock,
        new CreateActivityRequestValidator(), new UpdateActivityRequestValidator(),
        new ActivityQueryValidator(), new SaveActivityScoreRequestValidator(Clock),
        NullLogger<ActivityService>.Instance);

    private Activity SetupActivity()
    {
        var activity = new Activity { Id = 7, Title = "Exercise", StartDate = Clock.Today.AddDays(-30) };
        _activities.Setup(r => r.GetByIdAsync(7, It.IsAny<CancellationToken>())).ReturnsAsync(activity);
        return activity;
    }

    [Fact]
    public async Task SaveScore_RepeatedLowScores_SuggestsHelp()
    {
        SetupActivity();
        _scores.Setup(r => r.GetRecentForActivityAsync(7, Clock.Today, ActivityService.StruggleWindow, It.IsAny<CancellationToken>()))
            .ReturnsAsync([new() { Score = 0 }, new() { Score = 1 }, new() { Score = 4 }, new() { Score = 0 }]);

        var result = await CreateService().SaveScoreAsync(7, new SaveActivityScoreRequest(Clock.Today, 0, null));

        Assert.True(result.SuggestHelp);
        Assert.Equal(0, result.Score.Score);
        _scores.Verify(r => r.AddAsync(It.IsAny<ActivityScore>(), It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SaveScore_GoodScore_DoesNotSuggestHelp()
    {
        SetupActivity();
        var result = await CreateService().SaveScoreAsync(7, new SaveActivityScoreRequest(Clock.Today, 4, "  felt great "));

        Assert.False(result.SuggestHelp);
        Assert.Equal("felt great", result.Score.Notes);
    }

    [Fact]
    public async Task SaveScore_ExistingScore_IsUpdatedNotDuplicated()
    {
        var activity = SetupActivity();
        var existing = new ActivityScore { Id = 3, ActivityId = 7, Activity = activity, EvaluationDate = Clock.Today, Score = 1 };
        _scores.Setup(r => r.GetForDateAsync(7, Clock.Today, It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        var result = await CreateService().SaveScoreAsync(7, new SaveActivityScoreRequest(Clock.Today, 5, null));

        Assert.Equal(3, result.Score.Id);
        Assert.Equal(5, existing.Score);
        _scores.Verify(r => r.AddAsync(It.IsAny<ActivityScore>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SaveScore_BeforeStartDate_ThrowsBusinessRule()
    {
        SetupActivity();
        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            CreateService().SaveScoreAsync(7, new SaveActivityScoreRequest(Clock.Today.AddDays(-31), 3, null)));
    }

    [Fact]
    public async Task SaveScore_InvalidScore_ThrowsValidation()
    {
        await Assert.ThrowsAsync<ValidationException>(() =>
            CreateService().SaveScoreAsync(7, new SaveActivityScoreRequest(Clock.Today, 6, null)));
    }

    [Fact]
    public async Task GetById_Missing_ThrowsNotFound()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => CreateService().GetByIdAsync(42));
    }
}
