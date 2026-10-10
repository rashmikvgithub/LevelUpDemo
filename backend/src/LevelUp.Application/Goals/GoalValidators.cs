using FluentValidation;
using LevelUp.Application.Common.Interfaces;
using LevelUp.Domain.Entities;

namespace LevelUp.Application.Goals;

public sealed class CreateGoalRequestValidator : AbstractValidator<CreateGoalRequest>
{
    public CreateGoalRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(Goal.TitleMaxLength);
        RuleFor(x => x.Description).MaximumLength(Goal.DescriptionMaxLength);
    }
}

public sealed class UpdateGoalRequestValidator : AbstractValidator<UpdateGoalRequest>
{
    public UpdateGoalRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(Goal.TitleMaxLength);
        RuleFor(x => x.Description).MaximumLength(Goal.DescriptionMaxLength);
    }
}

public sealed class CreateGoalCheckpointRequestValidator : AbstractValidator<CreateGoalCheckpointRequest>
{
    public CreateGoalCheckpointRequestValidator(IDateTimeProvider clock)
    {
        RuleFor(x => x.PercentComplete).InclusiveBetween(0, 100);
        RuleFor(x => x.Note).MaximumLength(GoalCheckpoint.NoteMaxLength);
        RuleFor(x => x.CheckpointDate)
            .Must(d => d <= clock.Today.AddDays(1))
            .When(x => x.CheckpointDate.HasValue)
            .WithMessage("A checkpoint cannot be dated in the future.");
    }
}

public sealed class CompleteGoalRequestValidator : AbstractValidator<CompleteGoalRequest>
{
    public CompleteGoalRequestValidator()
    {
        RuleFor(x => x.Note).MaximumLength(GoalCheckpoint.NoteMaxLength);
    }
}

public sealed class GoalQueryValidator : AbstractValidator<GoalQuery>
{
    public GoalQueryValidator()
    {
        RuleFor(x => x.Search).MaximumLength(Goal.TitleMaxLength);
        RuleFor(x => x.Status).IsInEnum().When(x => x.Status.HasValue);
    }
}
