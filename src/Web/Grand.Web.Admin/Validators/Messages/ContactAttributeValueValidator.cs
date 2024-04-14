using FluentValidation;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Marketing.Contacts;
using Grand.Domain.Catalog;
using Grand.Infrastructure.Validators;
using Grand.Web.Admin.Models.Messages;

namespace Grand.Web.Admin.Validators.Messages;

public class ContactAttributeValueValidator : BaseGrandValidator<ContactAttributeValueModel>
{
    public ContactAttributeValueValidator(
        IEnumerable<IValidatorConsumer<ContactAttributeValueModel>> validators,
        IContactAttributeService contactAttributeService,
        ITranslationService translationService)
        : base(validators)
    {
        RuleFor(x => x.Name).NotEmpty()
            .WithMessage(
                translationService.GetResource(
                    "Admin.Catalog.Attributes.ContactAttributes.Values.Fields.Name.Required"));
        RuleFor(x => x).CustomAsync(async (x, context, _) =>
        {
            var contactAttribute = await contactAttributeService.GetContactAttributeById(x.ContactAttributeId);
            if (contactAttribute is { AttributeControlType: AttributeControlType.ColorSquares }
                && string.IsNullOrEmpty(x.ColorSquaresRgb))
                context.AddFailure("Color is required");
        });
    }
}