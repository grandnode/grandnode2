using Grand.Business.Checkout.Validators;
using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Utilities.Checkout;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Orders;
using Grand.Infrastructure;
using Grand.Infrastructure.Validators;
using MediatR;

namespace Grand.Business.Checkout.Services.Orders;

public class ShoppingCartValidator : IShoppingCartValidator
{
    private readonly IMediator _mediator;
    private readonly IValidatorFactory _validatorFactory;

    private readonly IWorkContext _workContext;

    public ShoppingCartValidator(
        IWorkContext workContext,
        IMediator mediator,
        IValidatorFactory validatorFactory)
    {
        _workContext = workContext;
        _mediator = mediator;
        _validatorFactory = validatorFactory;
    }

    public virtual async Task<IList<string>> GetStandardWarnings(Customer customer, Product product,
        ShoppingCartItem shoppingCartItem)
    {
        ArgumentNullException.ThrowIfNull(customer);
        ArgumentNullException.ThrowIfNull(product);

        var warnings = new List<string>();

        var result = await _validatorFactory.GetValidator<ShoppingCartStandardValidatorRecord>()
            .ValidateAsync(new ShoppingCartStandardValidatorRecord(customer, product, shoppingCartItem));
        if (!result.IsValid)
            warnings.AddRange(result.Errors.Select(x => x.ErrorMessage));

        return warnings;
    }

    public virtual async Task<IList<string>> GetShoppingCartItemAttributeWarnings(Customer customer, Product product,
        ShoppingCartItem shoppingCartItem, bool ignoreNonCombinableAttributes = false)
    {
        ArgumentNullException.ThrowIfNull(product);

        var warnings = new List<string>();

        var result = await _validatorFactory.GetValidator<ShoppingCartItemAttributeValidatorRecord>()
            .ValidateAsync(new ShoppingCartItemAttributeValidatorRecord(customer, product, shoppingCartItem,
                ignoreNonCombinableAttributes));
        if (!result.IsValid)
            warnings.AddRange(result.Errors.Select(x => x.ErrorMessage));

        return warnings;
    }


    public virtual async Task<IList<string>> GetShoppingCartItemGiftVoucherWarnings(Customer customer,
        Product product, ShoppingCartItem shoppingCartItem)
    {
        ArgumentNullException.ThrowIfNull(product);

        var warnings = new List<string>();

        //gift vouchers
        if (!product.IsGiftVoucher) return warnings;
        var result = await _validatorFactory.GetValidator<ShoppingCartGiftVoucherValidatorRecord>()
            .ValidateAsync(new ShoppingCartGiftVoucherValidatorRecord(customer, product, shoppingCartItem));
        if (!result.IsValid)
            warnings.AddRange(result.Errors.Select(x => x.ErrorMessage));

        return warnings;
    }

    public virtual async Task<IList<string>> GetInventoryProductWarnings(Customer customer, Product product,
        ShoppingCartItem shoppingCartItem)
    {
        var warnings = new List<string>();

        var result = await _validatorFactory.GetValidator<ShoppingCartInventoryProductValidatorRecord>()
            .ValidateAsync(new ShoppingCartInventoryProductValidatorRecord(customer, product, shoppingCartItem));
        if (!result.IsValid)
            warnings.AddRange(result.Errors.Select(x => x.ErrorMessage));

        return warnings;
    }


    public virtual async Task<IList<string>> GetAuctionProductWarning(double bid, Product product, Customer customer)
    {
        var warnings = new List<string>();
        if (product.ProductTypeId != ProductType.Auction) return warnings;
        var result = await _validatorFactory.GetValidator<ShoppingCartAuctionValidatorRecord>()
            .ValidateAsync(new ShoppingCartAuctionValidatorRecord(customer, product, null, bid));
        if (!result.IsValid)
            warnings.AddRange(result.Errors.Select(x => x.ErrorMessage));

        return warnings;
    }

    public virtual async Task<IList<string>> GetReservationProductWarnings(Customer customer, Product product,
        ShoppingCartItem shoppingCartItem)
    {
        var warnings = new List<string>();

        if (product.ProductTypeId != ProductType.Reservation)
            return warnings;

        var result = await _validatorFactory.GetValidator<ShoppingCartReservationProductValidatorRecord>()
            .ValidateAsync(new ShoppingCartReservationProductValidatorRecord(customer, product, shoppingCartItem));
        if (!result.IsValid)
            warnings.AddRange(result.Errors.Select(x => x.ErrorMessage));

        return warnings;
    }

    public virtual async Task<IList<string>> GetShoppingCartWarnings(IList<ShoppingCartItem> shoppingCart,
        IList<CustomAttribute> checkoutAttributes, bool validateCheckoutAttributes, bool validateAmount)
    {
        var warnings = new List<string>();
        checkoutAttributes ??= new List<CustomAttribute>();

        var result = await _validatorFactory.GetValidator<ShoppingCartWarningsValidatorRecord>()
            .ValidateAsync(new ShoppingCartWarningsValidatorRecord(_workContext.CurrentCustomer,
                _workContext.CurrentStore, shoppingCart));
        if (!result.IsValid)
            warnings.AddRange(result.Errors.Select(x => x.ErrorMessage));

        //validate checkout attributes
        if (validateCheckoutAttributes)
        {
            var resultCheckoutAttributes = await _validatorFactory
                .GetValidator<ShoppingCartCheckoutAttributesValidatorRecord>().ValidateAsync(
                    new ShoppingCartCheckoutAttributesValidatorRecord(_workContext.CurrentCustomer,
                        _workContext.CurrentStore,
                        shoppingCart, checkoutAttributes));
            if (!resultCheckoutAttributes.IsValid)
                warnings.AddRange(resultCheckoutAttributes.Errors.Select(x => x.ErrorMessage));
        }

        //validate subtotal/total amount in the cart
        if (validateAmount)
        {
            var resultCheckoutAttributes = await _validatorFactory
                .GetValidator<ShoppingCartTotalAmountValidatorRecord>().ValidateAsync(
                    new ShoppingCartTotalAmountValidatorRecord(_workContext.CurrentCustomer,
                        _workContext.WorkingCurrency, shoppingCart));
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
        int quantity, string reservationId)
    {
        var warnings = new List<string>();

        var result = await _validatorFactory.GetValidator<ShoppingCartCommonWarningsValidatorRecord>().ValidateAsync(
            new ShoppingCartCommonWarningsValidatorRecord(customer, _workContext.CurrentStore, currentCart,
                product, shoppingCartType, rentalStartDate, rentalEndDate, quantity, reservationId));
        if (!result.IsValid)
            warnings.AddRange(result.Errors.Select(x => x.ErrorMessage));

        return warnings;
    }

    public virtual async Task<IList<string>> GetShoppingCartItemWarnings(Customer customer,
        ShoppingCartItem shoppingCartItem,
        Product product, ShoppingCartValidatorOptions options)
    {
        ArgumentNullException.ThrowIfNull(product);

        var warnings = new List<string>();

        //standard properties
        if (options.GetStandardWarnings)
            warnings.AddRange(await GetStandardWarnings(customer, product, shoppingCartItem));

        //inventory properties
        if (options.GetInventoryWarnings)
            warnings.AddRange(await GetInventoryProductWarnings(customer, product, shoppingCartItem));

        //selected attributes
        if (options.GetAttributesWarnings)
            warnings.AddRange(await GetShoppingCartItemAttributeWarnings(customer, product, shoppingCartItem));

        //gift vouchers
        if (options.GetGiftVoucherWarnings)
            warnings.AddRange(await GetShoppingCartItemGiftVoucherWarnings(customer, product, shoppingCartItem));

        //required products
        if (options.GetRequiredProductWarnings)
            warnings.AddRange(await GetRequiredProductWarnings(customer, shoppingCartItem, product,
                shoppingCartItem.StoreId));

        //reservation products
        if (options.GetReservationWarnings)
            warnings.AddRange(await GetReservationProductWarnings(customer, product, shoppingCartItem));

        //event notification
        await _mediator.ShoppingCartItemWarningsAdded(warnings, customer, shoppingCartItem, product);

        return warnings;
    }

    public virtual async Task<IList<string>> GetRequiredProductWarnings(Customer customer,
        ShoppingCartItem shoppingCartItem, Product product, string storeId)
    {
        ArgumentNullException.ThrowIfNull(customer);
        ArgumentNullException.ThrowIfNull(product);

        var warnings = new List<string>();

        if (!product.RequireOtherProducts) return warnings;

        var result = await _validatorFactory.GetValidator<ShoppingCartRequiredProductValidatorRecord>()
            .ValidateAsync(new ShoppingCartRequiredProductValidatorRecord(_workContext.CurrentCustomer,
                _workContext.CurrentStore, product, shoppingCartItem));
        if (!result.IsValid)
            warnings.AddRange(result.Errors.Select(x => x.ErrorMessage));
        return warnings;
    }
}