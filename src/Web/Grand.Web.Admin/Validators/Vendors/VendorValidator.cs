using FluentValidation;
using Grand.Infrastructure.Validators;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Web.Admin.Models.Vendors;
using Grand.Web.Admin.Validators.Common;
using System.Collections.Generic;

namespace Grand.Web.Admin.Validators.Vendors
{
    public class VendorValidator : BaseGrandValidator<VendorModel>
    {
        public VendorValidator(
            IEnumerable<IValidatorConsumer<VendorModel>> validators,
            ITranslationService translationService)
            : base(validators)
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage(translationService.GetResource("Admin.Vendors.Fields.Name.Required"));
            RuleFor(x => x.Email).NotEmpty().WithMessage(translationService.GetResource("Admin.Vendors.Fields.Email.Required"));
            RuleFor(x => x.Email).EmailAddress().WithMessage(translationService.GetResource("Admin.Common.WrongEmail"));
            RuleFor(x => x.Commission)
                .Must(CommonValid.IsCommissionValid)
                .WithMessage(translationService.GetResource("Admin.Vendors.Fields.Commission.IsCommissionValid"));
        }
    }
}