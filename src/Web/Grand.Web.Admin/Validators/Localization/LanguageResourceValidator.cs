using FluentValidation;
using Grand.Infrastructure.Validators;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Web.Admin.Models.Localization;
using System.Collections.Generic;

namespace Grand.Web.Admin.Validators.Localization
{
    public class LanguageResourceValidator : BaseGrandValidator<LanguageResourceModel>
    {
        public LanguageResourceValidator(
            IEnumerable<IValidatorConsumer<LanguageResourceModel>> validators,
            ITranslationService translationService)
            : base(validators)
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage(translationService.GetResource("Admin.Configuration.Languages.Resources.Fields.Name.Required"));
            RuleFor(x => x.Value).NotEmpty().WithMessage(translationService.GetResource("Admin.Configuration.Languages.Resources.Fields.Value.Required"));
        }
    }
}