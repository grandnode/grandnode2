using FluentValidation;
using Grand.Domain.Vendors;
using Grand.Infrastructure.Validators;
using Grand.Business.Common.Interfaces.Directory;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Web.Models.Vendors;
using Grand.Web.Validators.Common;
using System.Collections.Generic;

namespace Grand.Web.Validators.Vendors
{
    public class VendorInfoValidator : BaseGrandValidator<VendorInfoModel>
    {
        public VendorInfoValidator(
            IEnumerable<IValidatorConsumer<VendorInfoModel>> validators,
            IEnumerable<IValidatorConsumer<VendorAddressModel>> addressvalidators,
            ITranslationService translationService, 
            ICountryService countryService, VendorSettings addressSettings)
            : base(validators)
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage(translationService.GetResource("Account.VendorInfo.Name.Required"));
            RuleFor(x => x.Email).NotEmpty().WithMessage(translationService.GetResource("Account.VendorInfo.Email.Required"));
            RuleFor(x => x.Email).EmailAddress().WithMessage(translationService.GetResource("Common.WrongEmail"));
            RuleFor(x => x.Address).SetValidator(new VendorAddressValidator(addressvalidators, translationService, countryService, addressSettings));
        }
    }
}