using FluentValidation;
using Grand.Domain.Vendors;
using Grand.Infrastructure.Validators;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Web.Models.Vendors;
using Grand.Web.Validators.Common;

namespace Grand.Web.Validators.Vendors
{
    public class ApplyVendorValidator : BaseGrandValidator<ApplyVendorModel>
    {
        public ApplyVendorValidator(
            IEnumerable<IValidatorConsumer<ApplyVendorModel>> validators,
            IEnumerable<IValidatorConsumer<VendorAddressModel>> addressvalidators,
            ITranslationService translationService, ICountryService countryService,
            VendorSettings addressSettings)
            : base(validators)
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage(translationService.GetResource("Vendors.ApplyAccount.Name.Required"));
            RuleFor(x => x.Email).NotEmpty().WithMessage(translationService.GetResource("Vendors.ApplyAccount.Email.Required"));
            RuleFor(x => x.Email).EmailAddress().WithMessage(translationService.GetResource("Common.WrongEmail"));
            RuleFor(x => x.Address).SetValidator(new VendorAddressValidator(addressvalidators, translationService, countryService, addressSettings));
        }
    }
}