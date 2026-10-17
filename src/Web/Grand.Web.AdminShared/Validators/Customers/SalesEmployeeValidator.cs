using FluentValidation;
using Grand.Infrastructure.Validators;
using Grand.Web.AdminShared.Models.Customers;
using Grand.Web.AdminShared.Validators.Common;
using Grand.Web.Common.Localization;

namespace Grand.Web.AdminShared.Validators.Customers;

public class SalesEmployeeValidator : BaseGrandValidator<SalesEmployeeModel>
{
    public SalesEmployeeValidator(
        IEnumerable<IValidatorConsumer<SalesEmployeeModel>> validators,
        IRequestTranslationService translationService)
        : base(validators)
    {
        RuleFor(x => x.Name).NotEmpty()
            .WithMessage(translationService.GetResource("Admin.Customers.SalesEmployee.Fields.Name.Required"));
        RuleFor(x => x.Commission)
            .Must(CommonValid.IsCommissionValid)
            .WithMessage(
                translationService.GetResource("Admin.Customers.SalesEmployee.Fields.Commission.IsCommissionValid"));
    }
}