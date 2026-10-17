using FluentValidation;
using Grand.Business.Core.Interfaces.Checkout.GiftVouchers;
using Grand.Infrastructure.Validators;
using Grand.Web.AdminShared.Models.Orders;
using Grand.Web.Common.Localization;

namespace Grand.Web.AdminShared.Validators.Orders;

public class GiftVoucherDeleteModelValidator : BaseGrandValidator<GiftVoucherDeleteModel>
{
    public GiftVoucherDeleteModelValidator(
        IEnumerable<IValidatorConsumer<GiftVoucherDeleteModel>> validators,
        IGiftVoucherService giftVoucherService, IRequestTranslationService translationService)
        : base(validators)
    {
        RuleFor(x => x).CustomAsync(async (x, context, _) =>
        {
            var giftVoucher = await giftVoucherService.GetGiftVoucherById(x.Id);
            if (giftVoucher.GiftVoucherUsageHistory.Any())
                context.AddFailure(translationService.GetResource("Admin.GiftVouchers.PreventDeleted"));
        });
    }
}