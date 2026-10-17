using FluentValidation;
using Grand.Infrastructure.Validators;
using Grand.Web.AdminShared.Models.Vendors;
using Grand.Web.AdminShared.Validators.Common;
using Grand.Web.Common.Localization;

namespace Grand.Web.AdminShared.Validators.Vendors;

public class VendorValidator : BaseGrandValidator<VendorModel>
{
    public VendorValidator(
        IEnumerable<IValidatorConsumer<VendorModel>> validators,
        IRequestTranslationService translationService)
        : base(validators)
    {
        RuleFor(x => x.Name).NotEmpty()
            .WithMessage(translationService.GetResource("Admin.Vendors.Fields.Name.Required"));
        RuleFor(x => x.Email).NotEmpty()
            .WithMessage(translationService.GetResource("Admin.Vendors.Fields.Email.Required"));
        RuleFor(x => x.Email).EmailAddress().WithMessage(translationService.GetResource("Admin.Common.WrongEmail"));
        RuleFor(x => x.Commission)
            .Must(CommonValid.IsCommissionValid)
            .WithMessage(translationService.GetResource("Admin.Vendors.Fields.Commission.IsCommissionValid"));
    }
}