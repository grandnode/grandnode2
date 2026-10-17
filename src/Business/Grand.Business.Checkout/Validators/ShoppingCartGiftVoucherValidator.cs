using FluentValidation;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Domain.Catalog;
using Grand.Domain.Customers;
using Grand.Domain.Localization;
using Grand.Domain.Orders;
using Grand.SharedKernel.Extensions;

namespace Grand.Business.Checkout.Validators;

public record ShoppingCartGiftVoucherValidatorRecord(
    Customer Customer,
    Product Product,
    ShoppingCartItem ShoppingCartItem,
    Language Language);

public class ShoppingCartGiftVoucherValidator : AbstractValidator<ShoppingCartGiftVoucherValidatorRecord>
{
    public ShoppingCartGiftVoucherValidator(ITranslationService translationService)
    {
        RuleFor(x => x).Custom((value, context) =>
        {
            GiftVoucherExtensions.GetGiftVoucherAttribute(value.ShoppingCartItem.Attributes,
                out var giftVoucherRecipientName, out var giftVoucherRecipientEmail,
                out var giftVoucherSenderName, out var giftVoucherSenderEmail, out _);

            if (string.IsNullOrEmpty(giftVoucherRecipientName))
                context.AddFailure(translationService.GetResource("ShoppingCart.RecipientNameError", value.Language.Id));

            if (value.Product.GiftVoucherTypeId == GiftVoucherType.Virtual)
                //validate for virtual gift vouchers only
                if (string.IsNullOrEmpty(giftVoucherRecipientEmail) ||
                    !CommonHelper.IsValidEmail(giftVoucherRecipientEmail))
                    context.AddFailure(translationService.GetResource("ShoppingCart.RecipientEmailError", value.Language.Id));

            if (string.IsNullOrEmpty(giftVoucherSenderName))
                context.AddFailure(translationService.GetResource("ShoppingCart.SenderNameError", value.Language.Id));

            if (value.Product.GiftVoucherTypeId != GiftVoucherType.Virtual) return;
            //validate for virtual gift vouchers only
            if (string.IsNullOrEmpty(giftVoucherSenderEmail) || !CommonHelper.IsValidEmail(giftVoucherSenderEmail))
                context.AddFailure(translationService.GetResource("ShoppingCart.SenderEmailError", value.Language.Id));
        });
    }
}