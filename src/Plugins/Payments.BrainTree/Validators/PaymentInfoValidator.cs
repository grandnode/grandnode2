using FluentValidation;
using Grand.Business.Common.Interfaces.Localization;
using Payments.BrainTree.Models;

namespace Payments.BrainTree.Validators
{
    public class PaymentInfoValidator : AbstractValidator<PaymentInfoModel>
    {
        public PaymentInfoValidator(BrainTreePaymentSettings brainTreePaymentSettings, ITranslationService translationService)
        {
            if (brainTreePaymentSettings.Use3DS)
                return;

            //useful links:
            //http://fluentvalidation.codeplex.com/wikipage?title=Custom&referringTitle=Documentation&ANCHOR#CustomValidator
            //http://benjii.me/2010/11/credit-card-validator-attribute-for-asp-net-mvc-3/

            RuleFor(x => x.CardholderName).NotEmpty().WithMessage(translationService.GetResource("Payment.CardholderName.Required"));
            RuleFor(x => x.CardNumber).CreditCard().WithMessage(translationService.GetResource("Payment.CardNumber.Wrong"));
            RuleFor(x => x.CardCode).Matches(@"^[0-9]{3,4}$").WithMessage(translationService.GetResource("Payment.CardCode.Wrong"));
            RuleFor(x => x.ExpireMonth).NotEmpty().WithMessage(translationService.GetResource("Payment.ExpireMonth.Required"));
            RuleFor(x => x.ExpireYear).NotEmpty().WithMessage(translationService.GetResource("Payment.ExpireYear.Required"));
        }
    }
}