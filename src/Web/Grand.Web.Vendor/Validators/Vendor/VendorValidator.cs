using FluentValidation;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Infrastructure.Validators;
using Grand.Web.Vendor.Models.Common;
using Grand.Web.Vendor.Models.Vendor;
using Grand.Web.Vendor.Validators.Common;

namespace Grand.Web.Vendor.Validators.Vendor;

public class VendorValidator : BaseGrandValidator<VendorModel>
{
    public VendorValidator(
        IEnumerable<IValidatorConsumer<VendorModel>> validators,
        IEnumerable<IValidatorConsumer<AddressModel>> addressValidators,
        ITranslationService translationService)
        : base(validators)
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage(translationService.GetResource("Vendor.Fields.Name.Required"));
        RuleFor(x => x.Email).NotEmpty().WithMessage(translationService.GetResource("Vendor.Fields.Email.Required"));
        RuleFor(x => x.Email).EmailAddress().WithMessage(translationService.GetResource("Vendor.Common.WrongEmail"));
        RuleFor(x => x.Address).SetValidator(new AddressValidator(addressValidators, translationService));
    }
}