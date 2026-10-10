namespace LevelUp.Domain.Enums;

public enum ActivityStatus
{
    Active = 1,
    Retired = 2
}

public enum GoalStatus
{
    NotStarted = 0,
    InProgress = 1,
    Completed = 2
}

public enum RecommendationCategory
{
    General = 0,
    Activity = 1,
    Goal = 2
}

public enum RecommendationTrigger
{
    /// <summary>User pressed the "Help Me!" button.</summary>
    Manual = 0,

    /// <summary>System prompted the user after repeated low scores / skips.</summary>
    LowScorePrompt = 1
}
