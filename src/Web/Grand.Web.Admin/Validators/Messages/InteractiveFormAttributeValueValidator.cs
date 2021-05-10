using FluentValidation;
using Grand.Infrastructure.Validators;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Web.Admin.Models.Messages;
using System.Collections.Generic;

namespace Grand.Web.Admin.Validators.Messages
{
    public class InteractiveFormAttributeValueValidator : BaseGrandValidator<InteractiveFormAttributeValueModel>
    {
        public InteractiveFormAttributeValueValidator(
            IEnumerable<IValidatorConsumer<InteractiveFormAttributeValueModel>> validators,
            ITranslationService translationService)
            : base(validators)
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage(translationService.GetResource("admin.marketing.InteractiveForms.Attribute.Values.Fields.Name.Required"));
        }
    }
}