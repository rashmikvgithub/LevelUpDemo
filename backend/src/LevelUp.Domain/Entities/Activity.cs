using LevelUp.Domain.Common;
using LevelUp.Domain.Enums;

namespace LevelUp.Domain.Entities;

/// <summary>
/// A recurring daily activity the user grades themselves on (e.g. "exercise", "read 10 pages").
/// </summary>
public class Activity : AuditableEntity
{
    public const int TitleMaxLength = 200;
    public const int DescriptionMaxLength = 1000;

    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ActivityStatus Status { get; set; } = ActivityStatus.Active;

    /// <summary>First day the activity is tracked ("date added").</summary>
    public DateOnly StartDate { get; set; }

    /// <summary>Day the activity was retired; null while active.</summary>
    public DateOnly? RetiredDate { get; set; }

    public ICollection<ActivityScore> Scores { get; set; } = new List<ActivityScore>();

    public bool IsTrackedOn(DateOnly date) =>
        StartDate <= date && (RetiredDate is null || RetiredDate >= date);

    public void Retire(DateOnly retiredDate)
    {
        Status = ActivityStatus.Retired;
        RetiredDate = retiredDate;
    }

    public void Reactivate()
    {
        Status = ActivityStatus.Active;
        RetiredDate = null;
    }
}
