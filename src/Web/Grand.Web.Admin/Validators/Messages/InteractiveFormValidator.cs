using FluentValidation;
using Grand.Infrastructure.Validators;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Web.Admin.Models.Messages;
using System.Collections.Generic;

namespace Grand.Web.Admin.Validators.Messages
{
    public class InteractiveFormValidator : BaseGrandValidator<InteractiveFormModel>
    {
        public InteractiveFormValidator(
            IEnumerable<IValidatorConsumer<InteractiveFormModel>> validators,
            ITranslationService translationService)
            : base(validators)
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage(translationService.GetResource("admin.marketing.InteractiveForms.Fields.Name.Required"));
            RuleFor(x => x.SystemName).NotEmpty().WithMessage(translationService.GetResource("admin.marketing.InteractiveForms.Fields.SystemName.Required"));
            RuleFor(x => x.Body).NotEmpty().WithMessage(translationService.GetResource("admin.marketing.InteractiveForms.Fields.Body.Required"));
        }
    }
}