using FluentValidation;
using Grand.Module.Api.DTOs.Customers;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Infrastructure;
using Grand.Infrastructure.Validators;

namespace Grand.Module.Api.Validators.Customers;

public class CustomerGroupValidator : BaseGrandValidator<CustomerGroupDto>
{
    public CustomerGroupValidator(
        IEnumerable<IValidatorConsumer<CustomerGroupDto>> validators,
        ITranslationService translationService,
        IContextAccessor contextAccessor, IGroupService groupService)
        : base(validators)
    {
        var languageId = contextAccessor.WorkContext.WorkingLanguage.Id;

        RuleFor(x => x.Name).NotEmpty()
            .WithMessage(translationService.GetResource("Api.Customers.CustomerGroup.Fields.Name.Required", languageId));

        RuleFor(x => x).MustAsync(async (x, _) =>
        {
            if (!string.IsNullOrEmpty(x.Id))
            {
                var group = await groupService.GetCustomerGroupById(x.Id);
                if (group == null)
                    return false;
            }

            return true;
        }).WithMessage(translationService.GetResource("Api.Customers.CustomerGroup.Fields.Id.NotExists", languageId));
        RuleFor(x => x).MustAsync(async (x, _) =>
        {
            if (!string.IsNullOrEmpty(x.Id))
            {
                var customerGroup = await groupService.GetCustomerGroupById(x.Id);
                if (customerGroup.IsSystem && !x.Active) return false;
            }

            return true;
        }).WithMessage(translationService.GetResource("Api.Customers.CustomerGroup.Fields.Active.CantEditSystem", languageId));
        RuleFor(x => x).MustAsync(async (x, _) =>
        {
            if (!string.IsNullOrEmpty(x.Id))
            {
                var customerGroup = await groupService.GetCustomerGroupById(x.Id);
                if (customerGroup.IsSystem &&
                    !customerGroup.SystemName.Equals(x.SystemName, StringComparison.OrdinalIgnoreCase)) return false;
            }

            return true;
        }).WithMessage(translationService.GetResource("Api.Customers.CustomerGroup.Fields.SystemName.CantEditSystem", languageId));
    }
}