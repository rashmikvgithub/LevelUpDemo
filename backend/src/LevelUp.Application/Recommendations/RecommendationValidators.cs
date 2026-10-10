using FluentValidation;
using LevelUp.Domain.Entities;
using LevelUp.Domain.Enums;

namespace LevelUp.Application.Recommendations;

public sealed class SaveRecommendationRequestValidator : AbstractValidator<SaveRecommendationRequest>
{
    public SaveRecommendationRequestValidator()
    {
        RuleFor(x => x.Text).NotEmpty().MaximumLength(Recommendation.TextMaxLength);
        RuleFor(x => x.Category).IsInEnum();
    }
}

public sealed class RecommendationRequestValidator : AbstractValidator<RecommendationRequest>
{
    public RecommendationRequestValidator()
    {
        RuleFor(x => x.Category).IsInEnum();
        RuleFor(x => x.Trigger).IsInEnum();
        RuleFor(x => x)
            .Must(x => !(x.ActivityId.HasValue && x.GoalId.HasValue))
            .WithName("Context")
            .WithMessage("Specify either an activity or a goal, not both.");
        RuleFor(x => x.ActivityId)
            .Null()
            .When(x => x.Category == RecommendationCategory.Goal)
            .WithMessage("Goal recommendations cannot reference an activity.");
        RuleFor(x => x.GoalId)
            .Null()
            .When(x => x.Category == RecommendationCategory.Activity)
            .WithMessage("Activity recommendations cannot reference a goal.");
    }
}
