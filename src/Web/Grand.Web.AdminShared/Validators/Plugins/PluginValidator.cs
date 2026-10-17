using FluentValidation;
using Grand.Infrastructure.Validators;
using Grand.Web.AdminShared.Models.Plugins;
using Grand.Web.Common.Localization;

namespace Grand.Web.AdminShared.Validators.Plugins;

public class PluginValidator : BaseGrandValidator<PluginModel>
{
    public PluginValidator(
        IEnumerable<IValidatorConsumer<PluginModel>> validators,
        IRequestTranslationService translationService)
        : base(validators)
    {
        RuleFor(x => x.FriendlyName).NotEmpty()
            .WithMessage(translationService.GetResource("Admin.Plugins.Fields.FriendlyName.Required"));
    }
}