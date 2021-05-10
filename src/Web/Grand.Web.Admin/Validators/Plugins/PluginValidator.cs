using FluentValidation;
using Grand.Infrastructure.Validators;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Web.Admin.Models.Plugins;
using System.Collections.Generic;

namespace Grand.Web.Admin.Validators.Plugins
{
    public class PluginValidator : BaseGrandValidator<PluginModel>
    {
        public PluginValidator(
            IEnumerable<IValidatorConsumer<PluginModel>> validators,
            ITranslationService translationService)
            : base(validators)
        {
            RuleFor(x => x.FriendlyName).NotEmpty().WithMessage(translationService.GetResource("Admin.Plugins.Fields.FriendlyName.Required"));
        }
    }
}