using FluentValidation;
using Grand.Infrastructure.Validators;
using Grand.Web.AdminShared.Models.Settings;
using Grand.Web.Common.Localization;

namespace Grand.Web.AdminShared.Validators.Settings;

public class MerchandiseReturnActionValidator : BaseGrandValidator<MerchandiseReturnActionModel>
{
    public MerchandiseReturnActionValidator(
        IEnumerable<IValidatorConsumer<MerchandiseReturnActionModel>> validators,
        IRequestTranslationService translationService)
        : base(validators)
    {
        RuleFor(x => x.Name).NotEmpty()
            .WithMessage(translationService.GetResource("Admin.Settings.Order.MerchandiseReturnActions.Name.Required"));
    }
}