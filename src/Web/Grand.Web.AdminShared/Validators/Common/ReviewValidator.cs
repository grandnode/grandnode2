using FluentValidation;
using Grand.Infrastructure.Validators;
using Grand.Web.AdminShared.Models.Common;
using Grand.Web.Common.Localization;

namespace Grand.Web.AdminShared.Validators.Common;

public class ReviewValidator : BaseGrandValidator<ReviewModel>
{
    public ReviewValidator(
        IEnumerable<IValidatorConsumer<ReviewModel>> validators,
        IRequestTranslationService translationService)
        : base(validators)
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .WithMessage(translationService.GetResource("Admin.Review.Fields.Title.Required"));
        RuleFor(x => x.ReviewText)
            .NotEmpty()
            .WithMessage(translationService.GetResource("Admin.Review.Fields.ReviewText.Required"));
    }
}