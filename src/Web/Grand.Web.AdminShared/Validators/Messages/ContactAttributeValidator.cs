using FluentValidation;
using Grand.Infrastructure.Validators;
using Grand.Web.AdminShared.Models.Messages;
using Grand.Web.Common.Localization;

namespace Grand.Web.AdminShared.Validators.Messages;

public class ContactAttributeValidator : BaseGrandValidator<ContactAttributeModel>
{
    public ContactAttributeValidator(
        IEnumerable<IValidatorConsumer<ContactAttributeModel>> validators,
        IRequestTranslationService translationService)
        : base(validators)
    {
        RuleFor(x => x.Name).NotEmpty()
            .WithMessage(
                translationService.GetResource("Admin.Catalog.Attributes.ContactAttributes.Fields.Name.Required"));
    }
}