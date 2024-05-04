using FluentValidation;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Infrastructure.Validators;
using Grand.Web.Admin.Models.Menu;

namespace Grand.Web.Admin.Validators.Common;

public class MenuValidator : BaseGrandValidator<MenuModel>
{
    public MenuValidator(
        IEnumerable<IValidatorConsumer<MenuModel>> validators,
        ITranslationService translationService)
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