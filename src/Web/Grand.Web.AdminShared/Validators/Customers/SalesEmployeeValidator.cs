using FluentValidation;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Infrastructure.Validators;
using Grand.Web.AdminShared.Models.Customers;
using Grand.Web.AdminShared.Validators.Common;

namespace Grand.Web.AdminShared.Validators.Customers;

public class SalesEmployeeValidator : BaseGrandValidator<SalesEmployeeModel>
{
    public SalesEmployeeValidator(
        IEnumerable<IValidatorConsumer<SalesEmployeeModel>> validators,
        ITranslationService translationService)
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