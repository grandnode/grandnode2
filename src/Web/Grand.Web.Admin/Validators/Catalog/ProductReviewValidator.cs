using FluentValidation;
using Grand.Infrastructure.Validators;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Web.Admin.Models.Catalog;
using System.Collections.Generic;

namespace Grand.Web.Admin.Validators.Catalog
{
    public class ProductReviewValidator : BaseGrandValidator<ProductReviewModel>
    {
        public ProductReviewValidator(
            IEnumerable<IValidatorConsumer<ProductReviewModel>> validators,
            ITranslationService translationService)
            : base(validators)
        {
            RuleFor(x => x.Title).NotEmpty().WithMessage(translationService.GetResource("Admin.Catalog.ProductReviews.Fields.Title.Required"));
            RuleFor(x => x.ReviewText).NotEmpty().WithMessage(translationService.GetResource("Admin.Catalog.ProductReviews.Fields.ReviewText.Required"));
        }}
}