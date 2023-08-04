using FluentValidation;
using Grand.Business.Core.Interfaces.Checkout.GiftVouchers;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Infrastructure.Validators;
using Grand.Web.Admin.Models.Orders;

namespace Grand.Web.Admin.Validators.Orders;

public class GiftVoucherDeleteModelValidator : BaseGrandValidator<GiftVoucherDeleteModel>
{
    public GiftVoucherDeleteModelValidator(
        IEnumerable<IValidatorConsumer<GiftVoucherDeleteModel>> validators,
        IGiftVoucherService giftVoucherService, ITranslationService translationService)
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