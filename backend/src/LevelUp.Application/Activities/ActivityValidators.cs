using FluentValidation;
using LevelUp.Application.Common.Interfaces;
using LevelUp.Domain.Entities;
using LevelUp.Domain.Enums;

namespace LevelUp.Application.Activities;

public sealed class CreateActivityRequestValidator : AbstractValidator<CreateActivityRequest>
{
    public CreateActivityRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(Activity.TitleMaxLength);
        RuleFor(x => x.Description).MaximumLength(Activity.DescriptionMaxLength);
    }
}

public sealed class UpdateActivityRequestValidator : AbstractValidator<UpdateActivityRequest>
{
    public UpdateActivityRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(Activity.TitleMaxLength);
        RuleFor(x => x.Description).MaximumLength(Activity.DescriptionMaxLength);
        RuleFor(x => x.Status).IsInEnum();
        RuleFor(x => x.RetiredDate)
            .GreaterThanOrEqualTo(x => x.StartDate)
            .When(x => x.RetiredDate.HasValue)
            .WithMessage("Retired date cannot be before the start date.");
        RuleFor(x => x.RetiredDate)
            .Null()
            .When(x => x.Status == ActivityStatus.Active)
            .WithMessage("An active activity cannot have a retired date.");
    }
}

public sealed class ActivityQueryValidator : AbstractValidator<ActivityQuery>
{
    public ActivityQueryValidator()
    {
        RuleFor(x => x.Search).MaximumLength(Activity.TitleMaxLength);
        RuleFor(x => x.Status).IsInEnum().When(x => x.Status.HasValue);
        RuleFor(x => x.AddedTo)
            .GreaterThanOrEqualTo(x => x.AddedFrom)
            .When(x => x.AddedFrom.HasValue && x.AddedTo.HasValue)
            .WithMessage("'Added To' must be on or after 'Added From'.");
        RuleFor(x => x.RetiredTo)
            .GreaterThanOrEqualTo(x => x.RetiredFrom)
            .When(x => x.RetiredFrom.HasValue && x.RetiredTo.HasValue)
            .WithMessage("'Retired To' must be on or after 'Retired From'.");
    }
}

public sealed class SaveActivityScoreRequestValidator : AbstractValidator<SaveActivityScoreRequest>
{
    public SaveActivityScoreRequestValidator(IDateTimeProvider clock)
    {
        RuleFor(x => x.Score).InclusiveBetween(ActivityScore.MinScore, ActivityScore.MaxScore);
        RuleFor(x => x.Notes).MaximumLength(ActivityScore.NotesMaxLength);
        // One day of slack accommodates clients in time zones ahead of the server.
        RuleFor(x => x.EvaluationDate)
            .Must(d => d <= clock.Today.AddDays(1))
            .WithMessage("You cannot grade an activity for a future date.");
    }
}
