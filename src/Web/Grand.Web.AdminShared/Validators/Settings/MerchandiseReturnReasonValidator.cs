using FluentValidation;
using Grand.Infrastructure.Validators;
using Grand.Web.AdminShared.Models.Settings;
using Grand.Web.Common.Localization;

namespace Grand.Web.AdminShared.Validators.Settings;

public class MerchandiseReturnReasonValidator : BaseGrandValidator<MerchandiseReturnReasonModel>
{
    public MerchandiseReturnReasonValidator(
        IEnumerable<IValidatorConsumer<MerchandiseReturnReasonModel>> validators,
        IRequestTranslationService translationService)
        : base(validators)
    {
        RuleFor(x => x.Name).NotEmpty()
            .WithMessage(translationService.GetResource("Admin.Settings.Order.MerchandiseReturnReasons.Name.Required"));
    }
}