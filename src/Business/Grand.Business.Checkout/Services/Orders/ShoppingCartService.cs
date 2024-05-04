using Grand.Business.Core.Commands.Checkout.Orders;
using Grand.Business.Core.Events.Checkout.ShoppingCart;
using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Utilities.Checkout;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Orders;
using Grand.Infrastructure;
using Grand.Infrastructure.Extensions;
using MediatR;

namespace Grand.Business.Checkout.Services.Orders;

/// <summary>
///     Shopping cart service
/// </summary>
public class ShoppingCartService : IShoppingCartService
{
    #region Ctor

    public ShoppingCartService(
        IWorkContext workContext,
        IProductService productService,
        ICustomerService customerService,
        IMediator mediator,
        IUserFieldService userFieldService,
        IShoppingCartValidator shoppingCartValidator,
        ShoppingCartSettings shoppingCartSettings)
    {
        _workContext = workContext;
        _productService = productService;
        _customerService = customerService;
        _mediator = mediator;
        _userFieldService = userFieldService;
        _shoppingCartValidator = shoppingCartValidator;
        _shoppingCartSettings = shoppingCartSettings;
    }

    #endregion

    #region Fields

    private readonly IWorkContext _workContext;
    private readonly IProductService _productService;
    private readonly ICustomerService _customerService;
    private readonly IMediator _mediator;
    private readonly IUserFieldService _userFieldService;
    private readonly IShoppingCartValidator _shoppingCartValidator;
    private readonly ShoppingCartSettings _shoppingCartSettings;

    #endregion

    #region Methods

    /// <summary>
    ///     Gets shopping cart
    /// </summary>
    /// <param name="storeId">Store identifier; pass null to load all records</param>
    /// <param name="shoppingCartType">Shopping cart type; pass null to load all records</param>
    /// <returns>Shopping Cart</returns>
    public virtual async Task<IList<ShoppingCartItem>> GetShoppingCart(string storeId = null,
        params ShoppingCartType[] shoppingCartType)
    {
        var model = new List<ShoppingCartItem>();
        var cart = _workContext.CurrentCustomer.ShoppingCartItems.ToList();

        if (!string.IsNullOrEmpty(storeId))
            cart = cart.LimitPerStore(_shoppingCartSettings.SharedCartBetweenStores, storeId).ToList();

        if (shoppingCartType.Length > 0)
            cart = cart.Where(sci => shoppingCartType.Contains(sci.ShoppingCartTypeId)).ToList();

        foreach (var item in cart)
        {
            var product = await _productService.GetProductById(item.ProductId);
            if (product == null)
                continue;

            model.Add(item);
        }

        return model;
    }

    /// <summary>
    ///     Finds a shopping cart item in the cart
    /// </summary>
    /// <param name="shoppingCart">Shopping cart</param>
    /// <param name="shoppingCartType">Shopping cart type</param>
    /// <param name="warehouseId"></param>
    /// <param name="attributes">Attributes</param>
    /// <param name="customerEnteredPrice">Price entered by a customer</param>
    /// <param name="rentalStartDate">Rental start date</param>
    /// <param name="rentalEndDate">Rental end date</param>
    /// <param name="productId"></param>
    /// <returns>Found shopping cart item</returns>
    public virtual async Task<ShoppingCartItem> FindShoppingCartItem(IList<ShoppingCartItem> shoppingCart,
        ShoppingCartType shoppingCartType,
        string productId,
        string warehouseId = null,
        IList<CustomAttribute> attributes = null,
        double? customerEnteredPrice = null,
        DateTime? rentalStartDate = null,
        DateTime? rentalEndDate = null)
    {
        ArgumentNullException.ThrowIfNull(shoppingCart);

        foreach (var sci in shoppingCart.Where(a => a.ShoppingCartTypeId == shoppingCartType))
        {
            if (sci.ProductId != productId || sci.WarehouseId != warehouseId) continue;
            //attributes
            var product = await _productService.GetProductById(sci.ProductId);
            var attributesEqual =
                ProductExtensions.AreProductAttributesEqual(product, sci.Attributes, attributes, false);
            if (product.ProductTypeId == ProductType.BundledProduct)
                foreach (var bundle in product.BundleProducts)
                {
                    var p1 = await _productService.GetProductById(bundle.ProductId);
                    if (p1 == null) continue;
                    if (!ProductExtensions.AreProductAttributesEqual(p1, sci.Attributes, attributes, false))
                        attributesEqual = false;
                }

            //gift vouchers
            var giftVoucherInfoSame = true;
            if (product.IsGiftVoucher)
            {
                GiftVoucherExtensions.GetGiftVoucherAttribute(attributes,
                    out var giftVoucherRecipientName1, out _,
                    out var giftVoucherSenderName1, out _, out _);

                GiftVoucherExtensions.GetGiftVoucherAttribute(sci.Attributes,
                    out var giftVoucherRecipientName2, out _,
                    out var giftVoucherSenderName2, out _, out _);

                if (!string.Equals(giftVoucherRecipientName1, giftVoucherRecipientName2,
                        StringComparison.InvariantCultureIgnoreCase) ||
                    !string.Equals(giftVoucherSenderName1, giftVoucherSenderName2,
                        StringComparison.InvariantCultureIgnoreCase))
                    giftVoucherInfoSame = false;
            }

            //price is the same (for products which require customers to enter a price)
            var customerEnteredPricesEqual = true;
            if (sci.EnteredPrice.HasValue)
            {
                if (customerEnteredPrice.HasValue)
                    customerEnteredPricesEqual = Math.Round(sci.EnteredPrice.Value, 2) ==
                                                 Math.Round(customerEnteredPrice.Value, 2);
                else
                    customerEnteredPricesEqual = false;
            }
            else
            {
                if (customerEnteredPrice.HasValue)
                    customerEnteredPricesEqual = false;
            }

            //found?
            if (attributesEqual && giftVoucherInfoSame && customerEnteredPricesEqual)
                return sci;
        }

        return null;
    }

    /// <summary>
    ///     Add a product to shopping cart
    /// </summary>
    /// <param name="customer">Customer</param>
    /// <param name="productId">Product id</param>
    /// <param name="shoppingCartType">ShoppingCartType</param>
    /// <param name="storeId">Store id</param>
    /// <param name="warehouseId">Warehouse id</param>
    /// <param name="attributes">Attributes</param>
    /// <param name="customerEnteredPrice">EnteredPrice</param>
    /// <param name="rentalStartDate">RentalStartDate</param>
    /// <param name="rentalEndDate">RentalEndDate</param>
    /// <param name="quantity">Quantity</param>
    /// <param name="automaticallyAddRequiredProductsIfEnabled">AutomaticallyAddRequiredProductsIfEnabled</param>
    /// <param name="reservationId">ReservationId</param>
    /// <param name="parameter">Parameter for reservation</param>
    /// <param name="duration">Duration for reservation</param>
    /// <param name="validator">ShoppingCartValidatorOptions</param>
    /// <returns>(warnings, shoppingCartItem)</returns>
    public virtual async Task<(IList<string> warnings, ShoppingCartItem shoppingCartItem)> AddToCart(Customer customer,
        string productId,
        ShoppingCartType shoppingCartType, string storeId,
        string warehouseId = null, IList<CustomAttribute> attributes = null,
        double? customerEnteredPrice = null,
        DateTime? rentalStartDate = null, DateTime? rentalEndDate = null,
        int quantity = 1, bool automaticallyAddRequiredProductsIfEnabled = true,
        string reservationId = "", string parameter = "", string duration = "",
        ShoppingCartValidatorOptions validator = null)
    {
        ArgumentNullException.ThrowIfNull(customer);

        validator ??= new ShoppingCartValidatorOptions();

        var product = await _productService.GetProductById(productId);
        if (product == null)
            throw new ArgumentNullException(nameof(product));

        var cart = customer.ShoppingCartItems
            .Where(sci => sci.ShoppingCartTypeId == shoppingCartType)
            .LimitPerStore(_shoppingCartSettings.SharedCartBetweenStores, storeId)
            .ToList();

        var warnings = (await _shoppingCartValidator.CheckCommonWarnings(customer, cart, product, shoppingCartType,
            rentalStartDate,
            rentalEndDate, quantity, reservationId)).ToList();

        if (warnings.Any())
            return (warnings, null);

        var shoppingCartItem = await FindShoppingCartItem(cart,
            shoppingCartType, productId, warehouseId, attributes, customerEnteredPrice,
            rentalStartDate, rentalEndDate);

        var update = shoppingCartItem != null && product.ProductTypeId != ProductType.Reservation
                                              && string.IsNullOrEmpty(product.AllowedQuantities);

        if (update) shoppingCartItem.Quantity += quantity;
        else
            shoppingCartItem = new ShoppingCartItem {
                ShoppingCartTypeId = shoppingCartType,
                StoreId = storeId,
                WarehouseId = warehouseId,
                ProductId = productId,
                Attributes = attributes,
                EnteredPrice = customerEnteredPrice,
                Quantity = quantity,
                RentalStartDateUtc = rentalStartDate,
                RentalEndDateUtc = rentalEndDate,
                AdditionalShippingChargeProduct = product.AdditionalShippingCharge,
                IsFreeShipping = product.IsFreeShipping,
                IsShipEnabled = product.IsShipEnabled,
                IsTaxExempt = product.IsTaxExempt,
                IsGiftVoucher = product.IsGiftVoucher,
                CreatedOnUtc = DateTime.UtcNow,
                ReservationId = reservationId,
                Parameter = parameter,
                Duration = duration
            };
        warnings.AddRange(
            await _shoppingCartValidator.GetShoppingCartItemWarnings(customer, shoppingCartItem, product, validator));

        if (warnings.Any()) return (warnings, shoppingCartItem);
        if (update) await UpdateExistingShoppingCartItem(shoppingCartItem, customer, attributes);
        else
            shoppingCartItem = await InsertNewItem(customer, product, shoppingCartItem,
                automaticallyAddRequiredProductsIfEnabled);

        if (product.ProductTypeId == ProductType.Reservation)
            await _mediator.Send(new AddCustomerReservationCommand {
                Customer = customer,
                Product = product,
                ShoppingCartItem = shoppingCartItem,
                RentalStartDate = rentalStartDate,
                RentalEndDate = rentalEndDate,
                ReservationId = reservationId
            });
        //reset checkout info
        await _customerService.ResetCheckoutData(customer, storeId);

        return (warnings, shoppingCartItem);
    }

    private async Task UpdateExistingShoppingCartItem(ShoppingCartItem shoppingCartItem, Customer customer,
        IList<CustomAttribute> attributes)
    {
        shoppingCartItem.Attributes = attributes;
        shoppingCartItem.UpdatedOnUtc = DateTime.UtcNow;
        await _customerService.UpdateShoppingCartItem(customer.Id, shoppingCartItem);

        //event notification
        await _mediator.EntityUpdated(shoppingCartItem);
    }

    private async Task<ShoppingCartItem> InsertNewItem(
        Customer customer, Product product, ShoppingCartItem shoppingCartItem,
        bool automaticallyAddRequiredProductsIfEnabled)
    {
        customer.ShoppingCartItems.Add(shoppingCartItem);
        await _customerService.InsertShoppingCartItem(customer.Id, shoppingCartItem);

        //event notification
        await _mediator.Publish(new AddToCartEvent(customer, shoppingCartItem, product));
        if (automaticallyAddRequiredProductsIfEnabled)
            await _mediator.Send(new AddRequiredProductsCommand {
                Customer = customer,
                Product = product,
                ShoppingCartType = shoppingCartItem.ShoppingCartTypeId,
                StoreId = shoppingCartItem.StoreId
            });

        return shoppingCartItem;
    }

    /// <summary>
    ///     Updates the shopping cart item
    /// </summary>
    /// <param name="customer">Customer</param>
    /// <param name="shoppingCartItemId">Shopping cart item identifier</param>
    /// <param name="warehouseId"></param>
    /// <param name="attributes">Attributes</param>
    /// <param name="customerEnteredPrice">New customer entered price</param>
    /// <param name="rentalStartDate">Rental start date</param>
    /// <param name="rentalEndDate">Rental end date</param>
    /// <param name="quantity">New shopping cart item quantity</param>
    /// <param name="resetCheckoutData">A value indicating whether to reset checkout data</param>
    /// <param name="reservationId"></param>
    /// <param name="sciId"></param>
    /// <returns>Warnings</returns>
    public virtual async Task<IList<string>> UpdateShoppingCartItem(Customer customer,
        string shoppingCartItemId, string warehouseId, IList<CustomAttribute> attributes,
        double? customerEnteredPrice,
        DateTime? rentalStartDate = null, DateTime? rentalEndDate = null,
        int quantity = 1, bool resetCheckoutData = true, string reservationId = "", string sciId = "")
    {
        ArgumentNullException.ThrowIfNull(customer);

        var warnings = new List<string>();

        var shoppingCartItem = customer.ShoppingCartItems.FirstOrDefault(sci => sci.Id == shoppingCartItemId);
        if (shoppingCartItem == null) return warnings;
        if (resetCheckoutData)
            //reset checkout data
            await _customerService.ResetCheckoutData(customer, shoppingCartItem.StoreId);
        if (quantity > 0)
        {
            var product = await _productService.GetProductById(shoppingCartItem.ProductId);
            shoppingCartItem.Quantity = quantity;
            shoppingCartItem.WarehouseId = warehouseId;
            shoppingCartItem.Attributes = attributes;
            shoppingCartItem.EnteredPrice = customerEnteredPrice;
            shoppingCartItem.RentalStartDateUtc = rentalStartDate;
            shoppingCartItem.RentalEndDateUtc = rentalEndDate;
            shoppingCartItem.UpdatedOnUtc = DateTime.UtcNow;
            shoppingCartItem.AdditionalShippingChargeProduct = product.AdditionalShippingCharge;
            shoppingCartItem.IsFreeShipping = product.IsFreeShipping;
            shoppingCartItem.IsShipEnabled = product.IsShipEnabled;
            shoppingCartItem.IsTaxExempt = product.IsTaxExempt;
            shoppingCartItem.IsGiftVoucher = product.IsGiftVoucher;
            //check warnings
            warnings.AddRange(await _shoppingCartValidator.GetShoppingCartItemWarnings(customer, shoppingCartItem,
                product, new ShoppingCartValidatorOptions()));
            if (warnings.Any()) return warnings;
            //if everything is OK, then update a shopping cart item
            await _customerService.UpdateShoppingCartItem(customer.Id, shoppingCartItem);

            //event notification
            await _mediator.EntityUpdated(shoppingCartItem);
        }
        else
        {
            //delete a shopping cart item
            await DeleteShoppingCartItem(customer, shoppingCartItem, resetCheckoutData, true);
        }

        return warnings;
    }

    /// <summary>
    ///     Delete shopping cart item
    /// </summary>
    /// <param name="customer"></param>
    /// <param name="shoppingCartItem">Shopping cart item</param>
    /// <param name="resetCheckoutData">A value indicating whether to reset checkout data</param>
    /// <param name="ensureOnlyActiveCheckoutAttributes">
    ///     A value indicating whether to ensure that only active checkout
    ///     attributes are attached to the current customer
    /// </param>
    public virtual async Task DeleteShoppingCartItem(Customer customer, ShoppingCartItem shoppingCartItem,
        bool resetCheckoutData = true,
        bool ensureOnlyActiveCheckoutAttributes = false)
    {
        ArgumentNullException.ThrowIfNull(shoppingCartItem);

        //reset checkout data
        if (resetCheckoutData) await _customerService.ResetCheckoutData(customer, shoppingCartItem.StoreId);

        //delete item
        customer.ShoppingCartItems.Remove(customer.ShoppingCartItems.FirstOrDefault(x => x.Id == shoppingCartItem.Id));
        await _customerService.DeleteShoppingCartItem(customer.Id, shoppingCartItem);

        //event notification
        await _mediator.EntityDeleted(shoppingCartItem);
    }


    /// <summary>
    ///     Migrate shopping cart
    /// </summary>
    /// <param name="fromCustomer">From customer</param>
    /// <param name="toCustomer">To customer</param>
    /// <param name="includeCouponCodes">
    ///     A value indicating whether to coupon codes (discount and gift voucher) should be also
    ///     re-applied
    /// </param>
    public virtual async Task MigrateShoppingCart(Customer fromCustomer, Customer toCustomer, bool includeCouponCodes)
    {
        ArgumentNullException.ThrowIfNull(fromCustomer);
        ArgumentNullException.ThrowIfNull(toCustomer);

        if (fromCustomer.Id == toCustomer.Id)
            return; //the same customer

        //shopping cart items
        var fromCart = fromCustomer.ShoppingCartItems.ToList();
        foreach (var sci in fromCart)
            await AddToCart(toCustomer, sci.ProductId, sci.ShoppingCartTypeId, sci.StoreId, sci.WarehouseId,
                sci.Attributes, sci.EnteredPrice,
                sci.RentalStartDateUtc, sci.RentalEndDateUtc, sci.Quantity, false, sci.ReservationId, sci.Parameter,
                sci.Duration,
                new ShoppingCartValidatorOptions());
        foreach (var sci in fromCart) await DeleteShoppingCartItem(fromCustomer, sci);

        //copy discount and gift voucher coupon codes
        if (includeCouponCodes)
        {
            //discount
            var coupons = fromCustomer.ParseAppliedCouponCodes(SystemCustomerFieldNames.DiscountCoupons);
            var resultCoupons = toCustomer.ApplyCouponCode(SystemCustomerFieldNames.DiscountCoupons, coupons);
            await _userFieldService.SaveField(toCustomer, SystemCustomerFieldNames.DiscountCoupons, resultCoupons);

            //gift voucher
            var giftVoucher = fromCustomer.ParseAppliedCouponCodes(SystemCustomerFieldNames.GiftVoucherCoupons);
            var resultGift = toCustomer.ApplyCouponCode(SystemCustomerFieldNames.GiftVoucherCoupons, giftVoucher);
            await _userFieldService.SaveField(toCustomer, SystemCustomerFieldNames.GiftVoucherCoupons, resultGift);
        }

        //copy url referer
        var lastUrlReferrer =
            await fromCustomer.GetUserField<string>(_userFieldService, SystemCustomerFieldNames.LastUrlReferrer);
        await _userFieldService.SaveField(toCustomer, SystemCustomerFieldNames.LastUrlReferrer, lastUrlReferrer);

        //move selected checkout attributes
        var checkoutAttributes = await fromCustomer.GetUserField<List<CustomAttribute>>(_userFieldService,
            SystemCustomerFieldNames.CheckoutAttributes, _workContext.CurrentStore.Id);
        await _userFieldService.SaveField(toCustomer, SystemCustomerFieldNames.CheckoutAttributes, checkoutAttributes,
            _workContext.CurrentStore.Id);
    }

    #endregion
}