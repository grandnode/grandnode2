using FluentValidation;
using Grand.Infrastructure.Validators;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Web.Admin.Models.Messages;
using System.Collections.Generic;

namespace Grand.Web.Admin.Validators.Messages
{
    public class InteractiveFormAttributeValidator : BaseGrandValidator<InteractiveFormAttributeModel>
    {
        public InteractiveFormAttributeValidator(
            IEnumerable<IValidatorConsumer<InteractiveFormAttributeModel>> validators,
            ITranslationService translationService)
            : base(validators)
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage(translationService.GetResource("admin.marketing.InteractiveForms.Attribute.Fields.Name.Required"));
            RuleFor(x => x.SystemName).NotEmpty().WithMessage(translationService.GetResource("admin.marketing.InteractiveForms.Attribute.Fields.SystemName.Required"));
        }
    }
}