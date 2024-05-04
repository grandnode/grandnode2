using FluentValidation;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Domain.Catalog;
using Grand.Domain.Customers;
using Grand.Domain.Orders;

namespace Grand.Business.Checkout.Validators;

public record ShoppingCartStandardValidatorRecord(
    Customer Customer,
    Product Product,
    ShoppingCartItem ShoppingCartItem);

public class ShoppingCartStandardValidator : AbstractValidator<ShoppingCartStandardValidatorRecord>
{
    public ShoppingCartStandardValidator(ITranslationService translationService, IAclService aclService)
    {
        RuleFor(x => x).Custom((value, context) =>
        {
            if (!value.Product.Published)
                context.AddFailure(translationService.GetResource("ShoppingCart.ProductUnpublished"));

            //we can't add grouped value.Product
            if (value.Product.ProductTypeId == ProductType.GroupedProduct)
                context.AddFailure("You can't add grouped value.Product");

            //ACL
            if (!aclService.Authorize(value.Product, value.Customer))
                context.AddFailure(translationService.GetResource("ShoppingCart.ProductUnpublished"));

            //Store acl
            if (!aclService.Authorize(value.Product, value.ShoppingCartItem.StoreId))
                context.AddFailure(translationService.GetResource("ShoppingCart.ProductUnpublished"));

            switch (value.ShoppingCartItem.ShoppingCartTypeId)
            {
                //disabled "add to cart" button
                case ShoppingCartType.ShoppingCart when value.Product.DisableBuyButton:
                    context.AddFailure(translationService.GetResource("ShoppingCart.BuyingDisabled"));
                    break;
                //disabled "add to wishlist" button
                case ShoppingCartType.Wishlist when value.Product.DisableWishlistButton:
                    context.AddFailure(translationService.GetResource("ShoppingCart.WishlistDisabled"));
                    break;
            }

            //call for price
            if (value.ShoppingCartItem.ShoppingCartTypeId == ShoppingCartType.ShoppingCart &&
                value.Product.CallForPrice) context.AddFailure(translationService.GetResource("Products.CallForPrice"));

            //customer entered price
            if (value.Product.EnteredPrice)
            {
                var shoppingCartItemEnteredPrice = value.ShoppingCartItem.EnteredPrice ?? 0;
                if (shoppingCartItemEnteredPrice < value.Product.MinEnteredPrice ||
                    shoppingCartItemEnteredPrice > value.Product.MaxEnteredPrice)
                    context.AddFailure(string.Format(
                        translationService.GetResource("ShoppingCart.CustomerEnteredPrice.RangeError"),
                        value.Product.MinEnteredPrice,
                        value.Product.MaxEnteredPrice));
            }

            //availability dates
            var availableStartDateError = false;
            if (value.Product.AvailableStartDateTimeUtc.HasValue)
            {
                var now = DateTime.UtcNow;
                var availableStartDateTime =
                    DateTime.SpecifyKind(value.Product.AvailableStartDateTimeUtc.Value, DateTimeKind.Utc);
                if (availableStartDateTime.CompareTo(now) > 0)
                {
                    context.AddFailure(translationService.GetResource("ShoppingCart.NotAvailable"));
                    availableStartDateError = true;
                }
            }

            if (!value.Product.AvailableEndDateTimeUtc.HasValue || availableStartDateError ||
                value.ShoppingCartItem.ShoppingCartTypeId != ShoppingCartType.ShoppingCart) return;
            {
                var now = DateTime.UtcNow;
                var availableEndDateTime =
                    DateTime.SpecifyKind(value.Product.AvailableEndDateTimeUtc.Value, DateTimeKind.Utc);
                if (availableEndDateTime.CompareTo(now) < 0)
                    context.AddFailure(translationService.GetResource("ShoppingCart.NotAvailable"));
            }
        });
    }
}