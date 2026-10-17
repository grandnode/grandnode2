using Grand.Business.Checkout.Validators;
using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Utilities.Checkout;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Localization;
using Grand.Domain.Orders;
using Grand.Infrastructure.Validators;
using Grand.Mediator;

namespace Grand.Business.Checkout.Services.Orders;

public class ShoppingCartValidator : IShoppingCartValidator
{
    private readonly IMediator _mediator;
    private readonly IValidatorFactory _validatorFactory;

    public ShoppingCartValidator(
        IMediator mediator,
        IValidatorFactory validatorFactory)
    {
        _mediator = mediator;
        _validatorFactory = validatorFactory;
    }

    public virtual async Task<IList<string>> GetStandardWarnings(Customer customer, Product product,
        ShoppingCartItem shoppingCartItem, Language language)
    {
        ArgumentNullException.ThrowIfNull(customer);
        ArgumentNullException.ThrowIfNull(product);
        ArgumentNullException.ThrowIfNull(language);

        var warnings = new List<string>();

        var result = await _validatorFactory.GetValidator<ShoppingCartStandardValidatorRecord>()
            .ValidateAsync(new ShoppingCartStandardValidatorRecord(customer, product, shoppingCartItem, language));
        if (!result.IsValid)
            warnings.AddRange(result.Errors.Select(x => x.ErrorMessage));

        return warnings;
    }

    public virtual async Task<IList<string>> GetShoppingCartItemAttributeWarnings(Customer customer, Product product,
        ShoppingCartItem shoppingCartItem, Language language, bool ignoreNonCombinableAttributes = false)
    {
        ArgumentNullException.ThrowIfNull(product);
        ArgumentNullException.ThrowIfNull(language);

        var warnings = new List<string>();

        var result = await _validatorFactory.GetValidator<ShoppingCartItemAttributeValidatorRecord>()
            .ValidateAsync(new ShoppingCartItemAttributeValidatorRecord(customer, product, shoppingCartItem,
                ignoreNonCombinableAttributes, language));
        if (!result.IsValid)
            warnings.AddRange(result.Errors.Select(x => x.ErrorMessage));

        return warnings;
    }


    public virtual async Task<IList<string>> GetShoppingCartItemGiftVoucherWarnings(Customer customer,
        Product product, ShoppingCartItem shoppingCartItem, Language language)
    {
        ArgumentNullException.ThrowIfNull(product);
        ArgumentNullException.ThrowIfNull(language);

        var warnings = new List<string>();

        //gift vouchers
        if (!product.IsGiftVoucher) return warnings;
        var result = await _validatorFactory.GetValidator<ShoppingCartGiftVoucherValidatorRecord>()
            .ValidateAsync(new ShoppingCartGiftVoucherValidatorRecord(customer, product, shoppingCartItem, language));
        if (!result.IsValid)
            warnings.AddRange(result.Errors.Select(x => x.ErrorMessage));

        return warnings;
    }

    public virtual async Task<IList<string>> GetInventoryProductWarnings(Customer customer, Product product,
        ShoppingCartItem shoppingCartItem, Language language)
    {
        ArgumentNullException.ThrowIfNull(language);

        var warnings = new List<string>();

        var result = await _validatorFactory.GetValidator<ShoppingCartInventoryProductValidatorRecord>()
            .ValidateAsync(new ShoppingCartInventoryProductValidatorRecord(customer, product, shoppingCartItem,
                language));
        if (!result.IsValid)
            warnings.AddRange(result.Errors.Select(x => x.ErrorMessage));

        return warnings;
    }


    public virtual async Task<IList<string>> GetAuctionProductWarning(double bid, Product product, Customer customer,
        Language language)
    {
        ArgumentNullException.ThrowIfNull(language);

        var warnings = new List<string>();
        if (product.ProductTypeId != ProductType.Auction) return warnings;
        var result = await _validatorFactory.GetValidator<ShoppingCartAuctionValidatorRecord>()
            .ValidateAsync(new ShoppingCartAuctionValidatorRecord(customer, product, null, bid, language));
        if (!result.IsValid)
            warnings.AddRange(result.Errors.Select(x => x.ErrorMessage));

        return warnings;
    }

    public virtual async Task<IList<string>> GetReservationProductWarnings(Customer customer, Product product,
        ShoppingCartItem shoppingCartItem, Language language)
    {
        ArgumentNullException.ThrowIfNull(language);

        var warnings = new List<string>();

        if (product.ProductTypeId != ProductType.Reservation)
            return warnings;

        var result = await _validatorFactory.GetValidator<ShoppingCartReservationProductValidatorRecord>()
            .ValidateAsync(new ShoppingCartReservationProductValidatorRecord(customer, product, shoppingCartItem,
                language));
        if (!result.IsValid)
            warnings.AddRange(result.Errors.Select(x => x.ErrorMessage));

        return warnings;
    }

    public virtual async Task<IList<string>> GetShoppingCartWarnings(IList<ShoppingCartItem> shoppingCart,
        IList<CustomAttribute> checkoutAttributes, CalculationContext context, Language language,
        bool validateCheckoutAttributes, bool validateAmount)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(language);

        var warnings = new List<string>();
        checkoutAttributes ??= new List<CustomAttribute>();

        var result = await _validatorFactory.GetValidator<ShoppingCartWarningsValidatorRecord>()
            .ValidateAsync(new ShoppingCartWarningsValidatorRecord(context.Customer, context.Store, shoppingCart,
                language));
        if (!result.IsValid)
            warnings.AddRange(result.Errors.Select(x => x.ErrorMessage));

        //validate checkout attributes
        if (validateCheckoutAttributes)
        {
            var resultCheckoutAttributes = await _validatorFactory
                .GetValidator<ShoppingCartCheckoutAttributesValidatorRecord>().ValidateAsync(
                    new ShoppingCartCheckoutAttributesValidatorRecord(context.Customer, context.Store,
                        shoppingCart, checkoutAttributes, language));
            if (!resultCheckoutAttributes.IsValid)
                warnings.AddRange(resultCheckoutAttributes.Errors.Select(x => x.ErrorMessage));
        }

        //validate subtotal/total amount in the cart
        if (validateAmount)
        {
            var resultCheckoutAttributes = await _validatorFactory
                .GetValidator<ShoppingCartTotalAmountValidatorRecord>().ValidateAsync(
                    new ShoppingCartTotalAmountValidatorRecord(shoppingCart, context, language));
            if (!resultCheckoutAttributes.IsValid)
                warnings.AddRange(resultCheckoutAttributes.Errors.Select(x => x.ErrorMessage));
        }

        //event notification
        await _mediator.ShoppingCartWarningsAdd(warnings, shoppingCart, checkoutAttributes, validateCheckoutAttributes);

        return warnings;
    }

    public async Task<IList<string>> CheckCommonWarnings(Customer customer, IList<ShoppingCartItem> currentCart,
        Product product,
        ShoppingCartType shoppingCartType, DateTime? rentalStartDate, DateTime? rentalEndDate,
        int quantity, string reservationId, Language language)
    {
        ArgumentNullException.ThrowIfNull(language);

        var warnings = new List<string>();

        var result = await _validatorFactory.GetValidator<ShoppingCartCommonWarningsValidatorRecord>().ValidateAsync(
            new ShoppingCartCommonWarningsValidatorRecord(customer, currentCart,
                product, shoppingCartType, rentalStartDate, rentalEndDate, quantity, reservationId, language));
        if (!result.IsValid)
            warnings.AddRange(result.Errors.Select(x => x.ErrorMessage));

        return warnings;
    }

    public virtual async Task<IList<string>> GetShoppingCartItemWarnings(Customer customer,
        ShoppingCartItem shoppingCartItem,
        Product product, ShoppingCartValidatorOptions options, Language language)
    {
        ArgumentNullException.ThrowIfNull(product);

        var warnings = new List<string>();

        //standard properties
        if (options.GetStandardWarnings)
            warnings.AddRange(await GetStandardWarnings(customer, product, shoppingCartItem, language));

        //inventory properties
        if (options.GetInventoryWarnings)
            warnings.AddRange(await GetInventoryProductWarnings(customer, product, shoppingCartItem, language));

        //selected attributes
        if (options.GetAttributesWarnings)
            warnings.AddRange(
                await GetShoppingCartItemAttributeWarnings(customer, product, shoppingCartItem, language));

        //gift vouchers
        if (options.GetGiftVoucherWarnings)
            warnings.AddRange(
                await GetShoppingCartItemGiftVoucherWarnings(customer, product, shoppingCartItem, language));

        //required products
        if (options.GetRequiredProductWarnings)
            warnings.AddRange(await GetRequiredProductWarnings(customer, shoppingCartItem, product,
                shoppingCartItem.StoreId, language));

        //reservation products
        if (options.GetReservationWarnings)
            warnings.AddRange(await GetReservationProductWarnings(customer, product, shoppingCartItem, language));

        //event notification
        await _mediator.ShoppingCartItemWarningsAdded(warnings, customer, shoppingCartItem, product);

        return warnings;
    }

    public virtual async Task<IList<string>> GetRequiredProductWarnings(Customer customer,
        ShoppingCartItem shoppingCartItem, Product product, string storeId, Language language)
    {
        ArgumentNullException.ThrowIfNull(customer);
        ArgumentNullException.ThrowIfNull(product);
        ArgumentNullException.ThrowIfNull(language);

        var warnings = new List<string>();

        if (!product.RequireOtherProducts) return warnings;

        var result = await _validatorFactory.GetValidator<ShoppingCartRequiredProductValidatorRecord>()
            .ValidateAsync(new ShoppingCartRequiredProductValidatorRecord(customer, storeId, product, shoppingCartItem,
                language));
        if (!result.IsValid)
            warnings.AddRange(result.Errors.Select(x => x.ErrorMessage));
        return warnings;
    }
}