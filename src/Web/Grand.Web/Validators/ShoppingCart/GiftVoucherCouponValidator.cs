using FluentValidation;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Queries.Checkout.Orders;
using Grand.Domain.Orders;
using Grand.Infrastructure;
using Grand.Infrastructure.Validators;
using Grand.Web.Models.ShoppingCart;
using MediatR;

namespace Grand.Web.Validators.ShoppingCart;

public class GiftVoucherCouponValidator : BaseGrandValidator<GiftVoucherCouponModel>
{
    public GiftVoucherCouponValidator(
        IEnumerable<IValidatorConsumer<GiftVoucherCouponModel>> validators,
        IMediator mediator, IWorkContext workContext,
        ITranslationService translationService)
        : base(validators)
    {
        RuleFor(x => x.GiftVoucherCouponCode).NotEmpty()
            .WithMessage(translationService.GetResource("ShoppingCart.Code.Required"));
        RuleFor(x => x).CustomAsync(async (x, context, _) =>
        {
            if (string.IsNullOrEmpty(x.GiftVoucherCouponCode))
                return;

            x.GiftVoucherCouponCode = x.GiftVoucherCouponCode.Trim();

            var giftVoucher =
                (await mediator.Send(
                    new GetGiftVoucherQuery { Code = x.GiftVoucherCouponCode, IsGiftVoucherActivated = true }, _))
                .FirstOrDefault();
            var isGiftVoucherValid = giftVoucher != null
                                     && giftVoucher.IsGiftVoucherValid(workContext.WorkingCurrency,
                                         workContext.CurrentStore);

            if (!isGiftVoucherValid)
                context.AddFailure(translationService.GetResource("ShoppingCart.Code.WrongGiftVoucher"));
        });
    }
}