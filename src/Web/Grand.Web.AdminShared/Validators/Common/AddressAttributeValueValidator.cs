using FluentValidation;
using Grand.Infrastructure.Validators;
using Grand.Web.AdminShared.Models.Common;
using Grand.Web.Common.Localization;

namespace Grand.Web.AdminShared.Validators.Common;

public class AddressAttributeValueValidator : BaseGrandValidator<AddressAttributeValueModel>
{
    public AddressAttributeValueValidator(
        IEnumerable<IValidatorConsumer<AddressAttributeValueModel>> validators,
        IRequestTranslationService translationService)
        : base(validators)
    {
        RuleFor(x => x.Name).NotEmpty()
            .WithMessage(translationService.GetResource("Admin.Address.AddressAttributes.Values.Fields.Name.Required"));
    }
}