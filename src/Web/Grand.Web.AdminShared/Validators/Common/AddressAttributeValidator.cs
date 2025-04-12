using FluentValidation;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Infrastructure.Validators;
using Grand.Web.AdminShared.Models.Common;

namespace Grand.Web.AdminShared.Validators.Common;

public class AddressAttributeValidator : BaseGrandValidator<AddressAttributeModel>
{
    public AddressAttributeValidator(
        IEnumerable<IValidatorConsumer<AddressAttributeModel>> validators,
        ITranslationService translationService)
        : base(validators)
    {
        RuleFor(x => x.Name).NotEmpty()
            .WithMessage(translationService.GetResource("Admin.Address.AddressAttributes.Fields.Name.Required"));
    }
}