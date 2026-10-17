using FluentValidation;
using Grand.Infrastructure.Validators;
using Grand.Web.AdminShared.Models.Orders;
using Grand.Web.Common.Localization;

namespace Grand.Web.AdminShared.Validators.Orders;

public class GiftVoucherValidator : BaseGrandValidator<GiftVoucherModel>
{
    public GiftVoucherValidator(
        IEnumerable<IValidatorConsumer<GiftVoucherModel>> validators,
        IRequestTranslationService translationService)
        : base(validators)
    {
        RuleFor(x => x.Code).NotEmpty()
            .WithMessage(translationService.GetResource("Admin.GiftVouchers.Fields.Code.Required"));
    }
}