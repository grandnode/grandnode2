using FluentValidation;
using Grand.Infrastructure.Validators;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Web.Models.Catalog;
using System.Collections.Generic;

namespace Grand.Web.Validators.Catalog
{
    public class ProductReviewsValidator : BaseGrandValidator<AddProductReviewModel>
    {
        public ProductReviewsValidator(
            IEnumerable<IValidatorConsumer<AddProductReviewModel>> validators,
            ITranslationService translationService)
            : base(validators)
        {
            RuleFor(x => x.Title).NotEmpty().WithMessage(translationService.GetResource("Reviews.Fields.Title.Required"));
            RuleFor(x => x.Title).Length(1, 200).WithMessage(string.Format(translationService.GetResource("Reviews.Fields.Title.MaxLengthValidation"), 200));
            RuleFor(x => x.ReviewText).NotEmpty().WithMessage(translationService.GetResource("Reviews.Fields.ReviewText.Required"));
        }
    }
}