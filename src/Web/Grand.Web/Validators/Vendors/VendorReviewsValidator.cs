using FluentValidation;
using Grand.Infrastructure.Validators;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Web.Models.Vendors;
using System.Collections.Generic;

namespace Grand.Web.Validators.Vendors
{
    public class VendorReviewsValidator : BaseGrandValidator<VendorReviewsModel>
    {
        public VendorReviewsValidator(
            IEnumerable<IValidatorConsumer<VendorReviewsModel>> validators,
            ITranslationService translationService)
            : base(validators)
        {
            RuleFor(x => x.AddVendorReview.Title).NotEmpty().WithMessage(translationService.GetResource("Reviews.Fields.Title.Required")).When(x => x.AddVendorReview != null);
            RuleFor(x => x.AddVendorReview.Title).Length(1, 200).WithMessage(string.Format(translationService.GetResource("Reviews.Fields.Title.MaxLengthValidation"), 200)).When(x => x.AddVendorReview != null && !string.IsNullOrEmpty(x.AddVendorReview.Title));
            RuleFor(x => x.AddVendorReview.ReviewText).NotEmpty().WithMessage(translationService.GetResource("Reviews.Fields.ReviewText.Required")).When(x => x.AddVendorReview != null);
        }
    }
}
