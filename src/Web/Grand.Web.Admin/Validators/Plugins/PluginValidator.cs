using FluentValidation;
using Grand.Infrastructure.Validators;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Web.Admin.Models.Plugins;

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