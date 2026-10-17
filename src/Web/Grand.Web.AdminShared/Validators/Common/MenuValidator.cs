using FluentValidation;
using Grand.Infrastructure.Validators;
using Grand.Web.AdminShared.Models.Menu;
using Grand.Web.Common.Localization;

namespace Grand.Web.AdminShared.Validators.Common;

public class MenuValidator : BaseGrandValidator<MenuModel>
{
    public MenuValidator(
        IEnumerable<IValidatorConsumer<MenuModel>> validators,
        IRequestTranslationService translationService)
        : base(validators)
    {
        RuleFor(x => x.SystemName)
            .NotEmpty()
            .WithMessage(translationService.GetResource("admin.configuration.menu.fields.SystemName.Required"));
        RuleFor(x => x.ResourceName)
            .NotEmpty()
            .WithMessage(translationService.GetResource("admin.configuration.menu.fields.ResourceName.Required"));
    }
}