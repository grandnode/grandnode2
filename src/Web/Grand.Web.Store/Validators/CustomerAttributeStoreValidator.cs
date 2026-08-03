using FluentValidation;
using Grand.Infrastructure.Validators;
using Grand.Web.AdminShared.Models.Customers;
using Grand.Web.Store.Models.Customers;

namespace Grand.Web.Store.Validators;

public class CustomerAttributeStoreValidator : BaseGrandValidator<CustomerAttributeStoreModel>
{
    public CustomerAttributeStoreValidator(
        IEnumerable<IValidatorConsumer<CustomerAttributeStoreModel>> validators,
        IValidator<CustomerAttributeModel> baseValidator)
        : base(validators)
    {
        Include(baseValidator);
    }
}
