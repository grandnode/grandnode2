using FluentValidation;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Infrastructure.Validators;
using Grand.Web.Admin.Models.Customers;

namespace Grand.Web.Admin.Validators.Customers;

public class CustomerGroupDeleteValidator : BaseGrandValidator<CustomerGroupDeleteModel>
{
    public CustomerGroupDeleteValidator(IEnumerable<IValidatorConsumer<CustomerGroupDeleteModel>> validators,
        IGroupService groupService)
        : base(validators)
    {
        RuleFor(x => x).CustomAsync(async (x, context, _) =>
        {
            var customerGroup = await groupService.GetCustomerGroupById(x.Id);
            if (customerGroup == null)
            {
                context.AddFailure("Not found with the specified id");
            }
            else
            {
                if (customerGroup!.IsSystem)
                    context.AddFailure("You can't delete system group");
            }
        });
    }
}