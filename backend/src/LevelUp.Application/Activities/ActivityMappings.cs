using LevelUp.Domain.Entities;

namespace LevelUp.Application.Activities;

internal static class ActivityMappings
{
    public static ActivityDto ToDto(this Activity a) =>
        new(a.Id, a.Title, a.Description, a.Status, a.StartDate, a.RetiredDate, a.CreatedDate, a.ModifiedDate);

    public static ActivityScoreDto ToDto(this ActivityScore s) =>
        new(s.Id, s.ActivityId, s.Activity.Title, s.Activity.Description, s.Activity.Status,
            s.EvaluationDate, s.Score, s.Notes, s.CreatedDate, s.ModifiedDate);
}
