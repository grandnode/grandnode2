using FluentValidation;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Domain.Catalog;
using Grand.Domain.Customers;
using Grand.Domain.Orders;

namespace Grand.Business.Checkout.Validators;

public record ShoppingCartAuctionValidatorRecord(
    Customer Customer,
    Product Product,
    ShoppingCartItem ShoppingCartItem,
    double Bid);

public class ShoppingCartAuctionValidator : AbstractValidator<ShoppingCartAuctionValidatorRecord>
{
    public ShoppingCartAuctionValidator(ITranslationService translationService)
    {
        RuleFor(x => x).Custom((value, context) =>
        {
            if (value.Bid <= value.Product.HighestBid || value.Bid <= value.Product.StartPrice)
                context.AddFailure(translationService.GetResource("ShoppingCart.BidMustBeHigher"));

            if (!value.Product.AvailableEndDateTimeUtc.HasValue)
                context.AddFailure(translationService.GetResource("ShoppingCart.NotAvailable"));

            if (value.Product.AvailableEndDateTimeUtc < DateTime.UtcNow)
                context.AddFailure(translationService.GetResource("ShoppingCart.NotAvailable"));
        });
    }
}