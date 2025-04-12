using FluentValidation;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Infrastructure.Validators;
using Grand.Web.AdminShared.Models.Vendors;
using Grand.Web.AdminShared.Validators.Common;

namespace Grand.Web.AdminShared.Validators.Vendors;

public class VendorValidator : BaseGrandValidator<VendorModel>
{
    public VendorValidator(
        IEnumerable<IValidatorConsumer<VendorModel>> validators,
        ITranslationService translationService)
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