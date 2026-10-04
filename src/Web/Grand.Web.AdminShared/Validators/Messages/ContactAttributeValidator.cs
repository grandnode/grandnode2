using FluentValidation;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Infrastructure.Validators;
using Grand.Web.AdminShared.Models.Messages;

namespace Grand.Web.AdminShared.Validators.Messages;

public class ContactAttributeValidator : BaseGrandValidator<ContactAttributeModel>
{
    public ContactAttributeValidator(
        IEnumerable<IValidatorConsumer<ContactAttributeModel>> validators,
        ITranslationService translationService)
        : base(validators)
    {
        RuleFor(x => x.Name).NotEmpty()
            .WithMessage(
                translationService.GetResource("Admin.Catalog.Attributes.ContactAttributes.Fields.Name.Required"));
    }
}