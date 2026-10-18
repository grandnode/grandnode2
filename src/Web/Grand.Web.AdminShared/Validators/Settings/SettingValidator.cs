using FluentValidation;
using Grand.Infrastructure.Validators;
using Grand.Web.AdminShared.Models.Settings;
using Grand.Web.Common.Localization;

namespace Grand.Web.AdminShared.Validators.Settings;

public class SettingValidator : BaseGrandValidator<SettingModel>
{
    public SettingValidator(
        IEnumerable<IValidatorConsumer<SettingModel>> validators,
        IRequestTranslationService translationService)
        : base(validators)
    {
        RuleFor(x => x.Name).NotEmpty()
            .WithMessage(translationService.GetResource("Admin.Settings.AllSettings.Fields.Name.Required"));
    }
}