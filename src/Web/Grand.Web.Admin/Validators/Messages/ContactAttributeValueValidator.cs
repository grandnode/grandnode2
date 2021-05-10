using FluentValidation;
using Grand.Infrastructure.Validators;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Web.Admin.Models.Messages;
using System.Collections.Generic;

namespace Grand.Web.Admin.Validators.Messages
{
    public class ContactAttributeValueValidator : BaseGrandValidator<ContactAttributeValueModel>
    {
        public ContactAttributeValueValidator(
            IEnumerable<IValidatorConsumer<ContactAttributeValueModel>> validators,
            ITranslationService translationService)
            : base(validators)
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage(translationService.GetResource("Admin.Catalog.Attributes.ContactAttributes.Values.Fields.Name.Required"));
        }
    }
}