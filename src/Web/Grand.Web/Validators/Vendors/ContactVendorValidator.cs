using FluentValidation;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Domain.Common;
using Grand.Infrastructure.Validators;
using Grand.Web.Models.Vendors;

namespace Grand.Web.Validators.Vendors
{
    public class ContactVendorValidator : BaseGrandValidator<ContactVendorModel>
    {
        public ContactVendorValidator(
            IEnumerable<IValidatorConsumer<ContactVendorModel>> validators,
            ITranslationService translationService, CommonSettings commonSettings)
            : base(validators)
        {
            RuleFor(x => x.Email).NotEmpty().WithMessage(translationService.GetResource("ContactVendor.Email.Required"));
            RuleFor(x => x.Email).EmailAddress().WithMessage(translationService.GetResource("Common.WrongEmail"));
            RuleFor(x => x.FullName).NotEmpty().WithMessage(translationService.GetResource("ContactVendor.FullName.Required"));
            if (commonSettings.SubjectFieldOnContactUsForm)
            {
                RuleFor(x => x.Subject).NotEmpty().WithMessage(translationService.GetResource("ContactVendor.Subject.Required"));
            }
            RuleFor(x => x.Enquiry).NotEmpty().WithMessage(translationService.GetResource("ContactVendor.Enquiry.Required"));
        }}
}