using FluentValidation;
using Grand.Infrastructure.Validators;
using Grand.Web.AdminShared.Models.Catalog;
using Grand.Web.Common.Localization;

namespace Grand.Web.AdminShared.Validators.Catalog;

public class ProductReviewValidator : BaseGrandValidator<ProductReviewModel>
{
    public ProductReviewValidator(
        IEnumerable<IValidatorConsumer<ProductReviewModel>> validators,
        IRequestTranslationService translationService)
        : base(validators)
    {
        RuleFor(x => x.Title).NotEmpty()
            .WithMessage(translationService.GetResource("Admin.Catalog.ProductReviews.Fields.Title.Required"));
        RuleFor(x => x.ReviewText).NotEmpty()
            .WithMessage(translationService.GetResource("Admin.Catalog.ProductReviews.Fields.ReviewText.Required"));
    }
}