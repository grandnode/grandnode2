using FluentValidation;
using Grand.Infrastructure.Validators;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Web.Admin.Models.Common;

namespace Grand.Web.Admin.Validators.Common
{
    public class ReviewValidator : BaseGrandValidator<ReviewModel>
    {
        public ReviewValidator(
            IEnumerable<IValidatorConsumer<ReviewModel>> validators,
            ITranslationService translationService)
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
}