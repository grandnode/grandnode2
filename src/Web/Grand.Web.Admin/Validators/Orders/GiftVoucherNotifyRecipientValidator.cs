using FluentValidation;
using Grand.Business.Core.Interfaces.Checkout.GiftVouchers;
using Grand.Domain.Catalog;
using Grand.Infrastructure.Validators;
using Grand.SharedKernel.Extensions;
using Grand.Web.Admin.Models.Orders;

namespace Grand.Web.Admin.Validators.Orders;

public class GiftVoucherNotifyRecipientValidator : BaseGrandValidator<GiftVoucherNotifyRecipient>
{
    public GiftVoucherNotifyRecipientValidator(
        IEnumerable<IValidatorConsumer<GiftVoucherNotifyRecipient>> validators,
        IGiftVoucherService giftVoucherService)
        : base(validators)
    {
        RuleFor(x => x).CustomAsync(async (x, context, _) =>
        {
            var giftVoucher = await giftVoucherService.GetGiftVoucherById(x.Id);
            if (giftVoucher.GiftVoucherTypeId == GiftVoucherType.Physical)
                context.AddFailure("Only virtual type gift voucher can notify recipient");

            if (!CommonHelper.IsValidEmail(giftVoucher.RecipientEmail))
                context.AddFailure("Recipient email is not valid");
            if (!CommonHelper.IsValidEmail(giftVoucher.SenderEmail))
                context.AddFailure("Sender email is not valid");
        });
    }
}