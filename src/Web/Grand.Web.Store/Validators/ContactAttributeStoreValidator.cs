using FluentValidation;
using Grand.Infrastructure.Validators;
using Grand.Web.AdminShared.Models.Messages;
using Grand.Web.Store.Models.Messages;

namespace Grand.Web.Store.Validators;

public class ContactAttributeStoreValidator : BaseGrandValidator<ContactAttributeStoreModel>
{
    public ContactAttributeStoreValidator(
        IEnumerable<IValidatorConsumer<ContactAttributeStoreModel>> validators,
        IValidator<ContactAttributeModel> baseValidator)
        : base(validators)
    {
        Include(baseValidator);
    }
}
