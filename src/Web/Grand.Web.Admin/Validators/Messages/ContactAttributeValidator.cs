using FluentValidation;
using Grand.Infrastructure.Validators;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Web.Admin.Models.Messages;
using System.Collections.Generic;

namespace Grand.Web.Admin.Validators.Messages
{
    public class ContactAttributeValidator : BaseGrandValidator<ContactAttributeModel>
    {
        public ContactAttributeValidator(
            IEnumerable<IValidatorConsumer<ContactAttributeModel>> validators,
            ITranslationService translationService)
            : base(validators)
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage(translationService.GetResource("Admin.Catalog.Attributes.ContactAttributes.Fields.Name.Required"));
        }
    }
}