using FluentValidation;
using Grand.Infrastructure.Validators;
using Grand.Web.AdminShared.Models.Common;
using Grand.Web.Store.Models.Common;

namespace Grand.Web.Store.Validators;

public class AddressAttributeStoreValidator : BaseGrandValidator<AddressAttributeStoreModel>
{
    public AddressAttributeStoreValidator(
        IEnumerable<IValidatorConsumer<AddressAttributeStoreModel>> validators,
        IValidator<AddressAttributeModel> baseValidator)
        : base(validators)
    {
        Include(baseValidator);
    }
}
