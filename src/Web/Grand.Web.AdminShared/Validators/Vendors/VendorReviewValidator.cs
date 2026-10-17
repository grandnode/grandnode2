using FluentValidation;
using Grand.Infrastructure.Validators;
using Grand.Web.AdminShared.Models.Vendors;
using Grand.Web.Common.Localization;

namespace Grand.Web.AdminShared.Validators.Vendors;

public class VendorReviewValidator : BaseGrandValidator<VendorReviewModel>
{
    public VendorReviewValidator(
        IEnumerable<IValidatorConsumer<VendorReviewModel>> validators,
        IRequestTranslationService translationService)
        : base(validators)
    {
        RuleFor(x => x.Title).NotEmpty()
            .WithMessage(translationService.GetResource("Admin.VendorReviews.Fields.Title.Required"));
        RuleFor(x => x.ReviewText).NotEmpty()
            .WithMessage(translationService.GetResource("Admin.VendorReviews.Fields.ReviewText.Required"));
    }
}