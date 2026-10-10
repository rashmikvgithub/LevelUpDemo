using LevelUp.Domain.Entities;

namespace LevelUp.Application.Goals;

internal static class GoalMappings
{
    public static GoalDto ToDto(this Goal g, bool includeCheckpoints = false)
    {
        var ordered = g.Checkpoints
            .OrderByDescending(c => c.CheckpointDate)
            .ThenByDescending(c => c.CreatedDate)
            .ThenByDescending(c => c.Id)
            .ToList();

        return new GoalDto(
            g.Id, g.Title, g.Description, g.TargetDate, g.ProgressPercentage, g.Status, g.CompletedDate,
            ordered.FirstOrDefault()?.CheckpointDate, ordered.Count, g.CreatedDate, g.ModifiedDate,
            includeCheckpoints ? ordered.Select(c => c.ToDto()).ToList() : null);
    }

    public static GoalCheckpointDto ToDto(this GoalCheckpoint c) =>
        new(c.Id, c.GoalId, c.CheckpointDate, c.PercentComplete, c.Note, c.CreatedDate);
}
