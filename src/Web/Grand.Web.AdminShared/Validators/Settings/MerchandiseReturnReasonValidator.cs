using FluentValidation;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Infrastructure.Validators;
using Grand.Web.AdminShared.Models.Settings;

namespace Grand.Web.AdminShared.Validators.Settings;

public class MerchandiseReturnReasonValidator : BaseGrandValidator<MerchandiseReturnReasonModel>
{
    public MerchandiseReturnReasonValidator(
        IEnumerable<IValidatorConsumer<MerchandiseReturnReasonModel>> validators,
        ITranslationService translationService)
        : base(validators)
    {
        RuleFor(x => x.Name).NotEmpty()
            .WithMessage(translationService.GetResource("Admin.Settings.Order.MerchandiseReturnReasons.Name.Required"));
    }
}