using FluentValidation;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Infrastructure.Validators;
using Grand.Web.Admin.Models.Customers;

namespace Grand.Web.Admin.Validators.Customers;

public class CustomerGroupValidator : BaseGrandValidator<CustomerGroupModel>
{
    public CustomerGroupValidator(
        IEnumerable<IValidatorConsumer<CustomerGroupModel>> validators,
        IGroupService groupService,
        ITranslationService translationService)
        : base(validators)
    {
        RuleFor(x => x.Name).NotEmpty()
            .WithMessage(translationService.GetResource("Admin.Customers.CustomerGroups.Fields.Name.Required"));
        RuleFor(x => x).CustomAsync(async (x, context, _) =>
        {
            var customerGroup = await groupService.GetCustomerGroupById(x.Id);
            if (customerGroup != null)
            {
                if (customerGroup.IsSystem && !x.Active)
                    context.AddFailure(
                        translationService.GetResource(
                            "Admin.Customers.CustomerGroups.Fields.Active.CantEditSystem"));

                if (customerGroup.IsSystem &&
                    !customerGroup.SystemName.Equals(x.SystemName, StringComparison.OrdinalIgnoreCase))
                    context.AddFailure(
                        translationService.GetResource(
                            "Admin.Customers.CustomerGroups.Fields.SystemName.CantEditSystem"));
            }
        });
    }
}