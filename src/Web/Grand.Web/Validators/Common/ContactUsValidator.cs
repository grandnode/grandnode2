using FluentValidation;
using Grand.Domain.Common;
using Grand.Infrastructure.Validators;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Web.Models.Common;
using System.Collections.Generic;

namespace Grand.Web.Validators.Common
{
    public class ContactUsValidator : BaseGrandValidator<ContactUsModel>
    {
        public ContactUsValidator(
            IEnumerable<IValidatorConsumer<ContactUsModel>> validators,
            ITranslationService translationService, CommonSettings commonSettings)
            : base(validators)
        {
            RuleFor(x => x.Email).NotEmpty().WithMessage(translationService.GetResource("ContactUs.Email.Required"));
            RuleFor(x => x.Email).EmailAddress().WithMessage(translationService.GetResource("Common.WrongEmail"));
            RuleFor(x => x.FullName).NotEmpty().WithMessage(translationService.GetResource("ContactUs.FullName.Required"));
            if (commonSettings.SubjectFieldOnContactUsForm)
            {
                RuleFor(x => x.Subject).NotEmpty().WithMessage(translationService.GetResource("ContactUs.Subject.Required"));
            }
            RuleFor(x => x.Enquiry).NotEmpty().WithMessage(translationService.GetResource("ContactUs.Enquiry.Required"));
        }}
}