using FluentValidation;
using Grand.Infrastructure.Validators;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Web.Admin.Models.Customers;
using Grand.Web.Admin.Validators.Common;

namespace Grand.Web.Admin.Validators.Directory
{
    public class SalesEmployeeValidator : BaseGrandValidator<SalesEmployeeModel>
    {
        public SalesEmployeeValidator(
            IEnumerable<IValidatorConsumer<SalesEmployeeModel>> validators,
            ITranslationService translationService)
            : base(validators)
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage(translationService.GetResource("Admin.Customers.SalesEmployee.Fields.Name.Required"));
            RuleFor(x => x.Commission)
                .Must(CommonValid.IsCommissionValid)
                .WithMessage(translationService.GetResource("Admin.Customers.SalesEmployee.Fields.Commission.IsCommissionValid"));
        }
    }
}