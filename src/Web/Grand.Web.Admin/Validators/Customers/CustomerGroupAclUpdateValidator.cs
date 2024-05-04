using FluentValidation;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Business.Core.Utilities.Common.Security;
using Grand.Infrastructure.Validators;
using Grand.Web.Admin.Models.Customers;

namespace Grand.Web.Admin.Validators.Customers;

public class CustomerGroupAclUpdateValidator : BaseGrandValidator<CustomerGroupAclUpdateModel>
{
    public CustomerGroupAclUpdateValidator(IEnumerable<IValidatorConsumer<CustomerGroupAclUpdateModel>> validators,
        IGroupService groupService, IPermissionService permissionService)
        : base(validators)
    {
        RuleFor(x => x).CustomAsync(async (x, context, _) =>
        {
            if (!await permissionService.Authorize(StandardPermission.ManageAcl))
                context.AddFailure("You don't have permission to the update");

            var customerGroup = await groupService.GetCustomerGroupById(x.CustomerGroupId);
            if (customerGroup == null)
                context.AddFailure("No customer group found with the specified id");

            var permissionRecord = await permissionService.GetPermissionById(x.Id);
            if (permissionRecord == null)
                context.AddFailure("No permission found with the specified id");
        });
    }
}