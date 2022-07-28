using FluentValidation;
using Grand.Infrastructure.Validators;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Web.Admin.Models.Vendors;

namespace Grand.Web.Admin.Validators.Vendors
{
    public class VendorReviewValidator : BaseGrandValidator<VendorReviewModel>
    {
        public VendorReviewValidator(
            IEnumerable<IValidatorConsumer<VendorReviewModel>> validators,
            ITranslationService translationService)
            : base(validators)
        {
            RuleFor(x => x.Title).NotEmpty().WithMessage(translationService.GetResource("Admin.VendorReviews.Fields.Title.Required"));
            RuleFor(x => x.ReviewText).NotEmpty().WithMessage(translationService.GetResource("Admin.VendorReviews.Fields.ReviewText.Required"));
        }
    }
}
