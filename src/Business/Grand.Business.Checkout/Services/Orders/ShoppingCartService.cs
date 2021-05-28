using Grand.Business.Catalog.Interfaces.Products;
using Grand.Business.Checkout.Commands.Models.Orders;
using Grand.Business.Checkout.Events.ShoppingCart;
using Grand.Business.Checkout.Extensions;
using Grand.Business.Checkout.Interfaces.CheckoutAttributes;
using Grand.Business.Checkout.Interfaces.Orders;
using Grand.Business.Common.Extensions;
using Grand.Business.Common.Interfaces.Directory;
using Grand.Business.Customers.Interfaces;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Orders;
using Grand.Infrastructure;
using Grand.Infrastructure.Extensions;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace Grand.Business.Checkout.Services.Orders
{
    /// <summary>
    /// Shopping cart service
    /// </summary>
    public partial class ShoppingCartService : IShoppingCartService
    {
        #region Fields

        private readonly IWorkContext _workContext;
        private readonly IProductService _productService;
        private readonly IProductAttributeParser _productAttributeParser;
        private readonly ICheckoutAttributeParser _checkoutAttributeParser;
        private readonly ICustomerService _customerService;
        private readonly IMediator _mediator;
        private readonly IUserFieldService _userFieldService;
        private readonly IProductReservationService _productReservationService;
        private readonly IShoppingCartValidator _shoppingCartValidator;
        private readonly ShoppingCartSettings _shoppingCartSettings;
        #endregion

        #region Ctor

        public ShoppingCartService(
            IWorkContext workContext,
            IProductService productService,
            IProductAttributeParser productAttributeParser,
            ICheckoutAttributeParser checkoutAttributeParser,
            ICustomerService customerService,
            IMediator mediator,
            IUserFieldService userFieldService,
            IProductReservationService productReservationService,
            IShoppingCartValidator shoppingCartValidator,
            ShoppingCartSettings shoppingCartSettings)
        {
            _workContext = workContext;
            _productService = productService;
            _productAttributeParser = productAttributeParser;
            _checkoutAttributeParser = checkoutAttributeParser;
            _customerService = customerService;
            _mediator = mediator;
            _userFieldService = userFieldService;
            _productReservationService = productReservationService;
            _shoppingCartValidator = shoppingCartValidator;
            _shoppingCartSettings = shoppingCartSettings;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Gets shopping cart
        /// </summary>
        /// <param name="storeId">Store identifier; pass null to load all records</param>
        /// <param name="shoppingCartType">Shopping cart type; pass null to load all records</param>
        /// <returns>Shopping Cart</returns>
        public IList<ShoppingCartItem> GetShoppingCart(string storeId = null, params ShoppingCartType[] shoppingCartType)
        {
            IEnumerable<ShoppingCartItem> cart = _workContext.CurrentCustomer.ShoppingCartItems;

            if (!string.IsNullOrEmpty(storeId))
                cart = cart.LimitPerStore(_shoppingCartSettings.SharedCartBetweenStores, storeId);

            if (shoppingCartType.Length > 0)
                cart = cart.Where(sci => shoppingCartType.Contains(sci.ShoppingCartTypeId));

            return cart.ToList();
        }

        /// <summary>
        /// Finds a shopping cart item in the cart
        /// </summary>
        /// <param name="shoppingCart">Shopping cart</param>
        /// <param name="shoppingCartType">Shopping cart type</param>
        /// <param name="product">Product</param>
        /// <param name="attributes">Attributes</param>
        /// <param name="customerEnteredPrice">Price entered by a customer</param>
        /// <param name="rentalStartDate">Rental start date</param>
        /// <param name="rentalEndDate">Rental end date</param>
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
            if (shoppingCart == null)
                throw new ArgumentNullException(nameof(shoppingCart));

            foreach (var sci in shoppingCart.Where(a => a.ShoppingCartTypeId == shoppingCartType))
            {
                if (sci.ProductId == productId && sci.WarehouseId == warehouseId)
                {
                    //attributes
                    var _product = await _productService.GetProductById(sci.ProductId);
                    bool attributesEqual = _productAttributeParser.AreProductAttributesEqual(_product, sci.Attributes, attributes, false);
                    if (_product.ProductTypeId == ProductType.BundledProduct)
                    {
                        foreach (var bundle in _product.BundleProducts)
                        {
                            var p1 = await _productService.GetProductById(bundle.ProductId);
                            if (p1 != null)
                            {
                                if (!_productAttributeParser.AreProductAttributesEqual(p1, sci.Attributes, attributes, false))
                                    attributesEqual = false;
                            }
                        }
                    }
                    //gift vouchers
                    bool giftVoucherInfoSame = true;
                    if (_product.IsGiftVoucher)
                    {
                        _productAttributeParser.GetGiftVoucherAttribute(attributes,
                            out var giftVoucherRecipientName1, out var giftVoucherRecipientEmail1,
                            out var giftVoucherSenderName1, out var giftVoucherSenderEmail1, out var giftVoucherMessage1);

                        _productAttributeParser.GetGiftVoucherAttribute(sci.Attributes,
                            out var giftVoucherRecipientName2, out var giftVoucherRecipientEmail2,
                            out var giftVoucherSenderName2, out var giftVoucherSenderEmail2, out var giftVoucherMessage2);

                        if (giftVoucherRecipientName1.ToLowerInvariant() != giftVoucherRecipientName2.ToLowerInvariant() ||
                            giftVoucherSenderName1.ToLowerInvariant() != giftVoucherSenderName2.ToLowerInvariant())
                            giftVoucherInfoSame = false;
                    }

                    //price is the same (for products which require customers to enter a price)
                    bool customerEnteredPricesEqual = true;
                    if (sci.EnteredPrice.HasValue)
                    {
                        if (customerEnteredPrice.HasValue)
                            customerEnteredPricesEqual = Math.Round(sci.EnteredPrice.Value, 2) == Math.Round(customerEnteredPrice.Value, 2);
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
            }

            return null;
        }


        /// <summary>
        /// Add a product to shopping cart
        /// </summary>
        /// <param name="customer">Customer</param>
        /// <param name="product">Product</param>
        /// <param name="shoppingCartType">Shopping cart type</param>
        /// <param name="storeId">Store identifier</param>
        /// <param name="attributes">Attributes</param>
        /// <param name="customerEnteredPrice">The price enter by a customer</param>
        /// <param name="rentalStartDate">Rental start date</param>
        /// <param name="rentalEndDate">Rental end date</param>
        /// <param name="quantity">Quantity</param>
        /// <param name="automaticallyAddRequiredProductsIfEnabled">Automatically add required products if enabled</param>
        /// <returns>Warnings</returns>
        public virtual async Task<IList<string>> AddToCart(Customer customer, string productId,
            ShoppingCartType shoppingCartType, string storeId,
            string warehouseId = null, IList<CustomAttribute> attributes = null,
            double? customerEnteredPrice = null,
            DateTime? rentalStartDate = null, DateTime? rentalEndDate = null,
            int quantity = 1, bool automaticallyAddRequiredProductsIfEnabled = true,
            string reservationId = "", string parameter = "", string duration = "",
            bool getRequiredProductWarnings = true)
        {
            if (customer == null)
                throw new ArgumentNullException(nameof(customer));

            var product = await _productService.GetProductById(productId);
            if (product == null)
                throw new ArgumentNullException(nameof(product));

            var cart = customer.ShoppingCartItems
               .Where(sci => sci.ShoppingCartTypeId == shoppingCartType)
               .LimitPerStore(_shoppingCartSettings.SharedCartBetweenStores, storeId)
               .ToList();

            var warnings = (await _shoppingCartValidator.CheckCommonWarnings(customer, cart, product, shoppingCartType, rentalStartDate,
                rentalEndDate, quantity, reservationId)).ToList();

            if (warnings.Any())
                return warnings;

            var shoppingCartItem = await FindShoppingCartItem(cart,
                shoppingCartType, productId, warehouseId, attributes, customerEnteredPrice,
                rentalStartDate, rentalEndDate);

            var update = shoppingCartItem != null && product.ProductTypeId != ProductType.Reservation
                && String.IsNullOrEmpty(product.AllowedQuantities);

            if (update)
            {
                shoppingCartItem.Quantity += quantity;
                warnings.AddRange(await _shoppingCartValidator.GetShoppingCartItemWarnings(customer, shoppingCartItem, product, new ShoppingCartValidatorOptions() { GetRequiredProductWarnings = getRequiredProductWarnings }));
            }
            else
            {
                DateTime now = DateTime.UtcNow;
                shoppingCartItem = new ShoppingCartItem
                {
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
                    CreatedOnUtc = now,
                    UpdatedOnUtc = now,
                    ReservationId = reservationId,
                    Parameter = parameter,
                    Duration = duration
                };

                warnings.AddRange(await _shoppingCartValidator.GetShoppingCartItemWarnings
                    (customer, shoppingCartItem, product,
                    new ShoppingCartValidatorOptions()
                    { GetRequiredProductWarnings = getRequiredProductWarnings }));
            }

            if (!warnings.Any())
            {
                if (update)
                {
                    //update existing shopping cart item
                    await UpdateExistingShoppingCartItem(shoppingCartItem, quantity, customer, product, attributes, getRequiredProductWarnings);
                }
                else
                {
                    //insert new item
                    shoppingCartItem = await InsertNewItem(customer, product, shoppingCartItem, automaticallyAddRequiredProductsIfEnabled);
                }
                await HandleCustomerReservation(customer, product, rentalStartDate, rentalEndDate, shoppingCartItem, reservationId);

                //reset checkout info
                await _customerService.ResetCheckoutData(customer, storeId);
            }

            return warnings;
        }

        private async Task UpdateExistingShoppingCartItem(ShoppingCartItem shoppingCartItem, int quantity, Customer customer,
            Product product, IList<CustomAttribute> attributes, bool getRequiredProductWarnings)
        {
            shoppingCartItem.Attributes = attributes;
            shoppingCartItem.UpdatedOnUtc = DateTime.UtcNow;
            await _customerService.UpdateShoppingCartItem(customer.Id, shoppingCartItem);

            //event notification
            await _mediator.EntityUpdated(shoppingCartItem);
        }

        private async Task<ShoppingCartItem> InsertNewItem(
            Customer customer, Product product, ShoppingCartItem shoppingCartItem, bool automaticallyAddRequiredProductsIfEnabled)
        {

            customer.ShoppingCartItems.Add(shoppingCartItem);
            await _customerService.InsertShoppingCartItem(customer.Id, shoppingCartItem);

            //event notification
            await _mediator.Publish(new AddToCartEvent(customer, shoppingCartItem, product));
            if (automaticallyAddRequiredProductsIfEnabled)
            {
                await _mediator.Send(new AddRequiredProductsCommand()
                {
                    Customer = customer,
                    Product = product,
                    ShoppingCartType = shoppingCartItem.ShoppingCartTypeId,
                    StoreId = shoppingCartItem.StoreId
                });
            }

            return shoppingCartItem;
        }

        private async Task HandleCustomerReservation(
            Customer customer,
            Product product,
            DateTime? rentalStartDate, DateTime? rentalEndDate,
            ShoppingCartItem shoppingCartItem,
            string reservationId)
        {

            if (rentalStartDate.HasValue && rentalEndDate.HasValue)
            {
                var reservations = await _productReservationService.GetProductReservationsByProductId(product.Id, true, null);
                var reserved = await _productReservationService.GetCustomerReservationsHelpers(_workContext.CurrentCustomer.Id);
                foreach (var item in reserved)
                {
                    var match = reservations.Where(x => x.Id == item.ReservationId).FirstOrDefault();
                    if (match != null)
                    {
                        reservations.Remove(match);
                    }
                }

                IGrouping<string, ProductReservation> groupToBook = null;

                var grouped = reservations.GroupBy(x => x.Resource);
                foreach (var group in grouped)
                {
                    bool groupCanBeBooked = true;
                    if (product.IncBothDate && product.IntervalUnitId == IntervalUnit.Day)
                    {
                        for (DateTime iterator = rentalStartDate.Value; iterator <= rentalEndDate.Value; iterator += new TimeSpan(24, 0, 0))
                        {
                            if (!group.Select(x => x.Date).Contains(iterator))
                            {
                                groupCanBeBooked = false;
                                break;
                            }
                        }
                    }
                    else
                    {
                        for (DateTime iterator = rentalStartDate.Value; iterator < rentalEndDate.Value; iterator += new TimeSpan(24, 0, 0))
                        {
                            if (!group.Select(x => x.Date).Contains(iterator))
                            {
                                groupCanBeBooked = false;
                                break;
                            }
                        }
                    }
                    if (groupCanBeBooked)
                    {
                        groupToBook = group;
                        break;
                    }
                }

                if (groupToBook != null)
                {
                    if (product.IncBothDate && product.IntervalUnitId == IntervalUnit.Day)
                    {
                        foreach (var item in groupToBook.Where(x => x.Date >= rentalStartDate && x.Date <= rentalEndDate))
                        {
                            await _productReservationService.InsertCustomerReservationsHelper(new CustomerReservationsHelper
                            {
                                CustomerId = customer.Id,
                                ReservationId = item.Id,
                                ShoppingCartItemId = shoppingCartItem.Id
                            });
                        }
                    }
                    else
                    {
                        foreach (var item in groupToBook.Where(x => x.Date >= rentalStartDate && x.Date < rentalEndDate))
                        {
                            await _productReservationService.InsertCustomerReservationsHelper(new CustomerReservationsHelper
                            {
                                CustomerId = customer.Id,
                                ReservationId = item.Id,
                                ShoppingCartItemId = shoppingCartItem.Id
                            });
                        }
                    }
                }

            }

            if (!string.IsNullOrEmpty(reservationId))
            {
                await _productReservationService.InsertCustomerReservationsHelper(new CustomerReservationsHelper
                {
                    CustomerId = customer.Id,
                    ReservationId = reservationId,
                    ShoppingCartItemId = shoppingCartItem.Id
                });
            }
        }

        /// <summary>
        /// Updates the shopping cart item
        /// </summary>
        /// <param name="customer">Customer</param>
        /// <param name="shoppingCartItemId">Shopping cart item identifier</param>
        /// <param name="attributes">Attributes</param>
        /// <param name="customerEnteredPrice">New customer entered price</param>
        /// <param name="rentalStartDate">Rental start date</param>
        /// <param name="rentalEndDate">Rental end date</param>
        /// <param name="quantity">New shopping cart item quantity</param>
        /// <param name="resetCheckoutData">A value indicating whether to reset checkout data</param>
        /// <returns>Warnings</returns>
        public virtual async Task<IList<string>> UpdateShoppingCartItem(Customer customer,
            string shoppingCartItemId, string warehouseId, IList<CustomAttribute> attributes,
            double? customerEnteredPrice,
            DateTime? rentalStartDate = null, DateTime? rentalEndDate = null,
            int quantity = 1, bool resetCheckoutData = true, string reservationId = "", string sciId = "")
        {
            if (customer == null)
                throw new ArgumentNullException(nameof(customer));

            var warnings = new List<string>();

            var shoppingCartItem = customer.ShoppingCartItems.FirstOrDefault(sci => sci.Id == shoppingCartItemId);
            if (shoppingCartItem != null)
            {
                if (resetCheckoutData)
                {
                    //reset checkout data
                    await _customerService.ResetCheckoutData(customer, shoppingCartItem.StoreId);
                }
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
                    warnings.AddRange(await _shoppingCartValidator.GetShoppingCartItemWarnings(customer, shoppingCartItem, product, new ShoppingCartValidatorOptions()));
                    if (!warnings.Any())
                    {
                        //if everything is OK, then update a shopping cart item
                        await _customerService.UpdateShoppingCartItem(customer.Id, shoppingCartItem);

                        //event notification
                        await _mediator.EntityUpdated(shoppingCartItem);
                    }
                }
                else
                {
                    //delete a shopping cart item
                    await DeleteShoppingCartItem(customer, shoppingCartItem, resetCheckoutData, true);
                }
            }

            return warnings;
        }

        /// <summary>
        /// Delete shopping cart item
        /// </summary>
        /// <param name="shoppingCartItem">Shopping cart item</param>
        /// <param name="resetCheckoutData">A value indicating whether to reset checkout data</param>
        /// <param name="ensureOnlyActiveCheckoutAttributes">A value indicating whether to ensure that only active checkout attributes are attached to the current customer</param>
        public virtual async Task DeleteShoppingCartItem(Customer customer, ShoppingCartItem shoppingCartItem, bool resetCheckoutData = true,
            bool ensureOnlyActiveCheckoutAttributes = false)
        {
            if (shoppingCartItem == null)
                throw new ArgumentNullException(nameof(shoppingCartItem));

            if ((shoppingCartItem.RentalStartDateUtc.HasValue && shoppingCartItem.RentalEndDateUtc.HasValue) || !string.IsNullOrEmpty(shoppingCartItem.ReservationId))
            {
                var reserved = await _productReservationService.GetCustomerReservationsHelperBySciId(shoppingCartItem.Id);
                foreach (var res in reserved)
                {
                    if (res.CustomerId == _workContext.CurrentCustomer.Id)
                    {
                        await _productReservationService.DeleteCustomerReservationsHelper(res);
                    }
                }
            }
            var storeId = shoppingCartItem.StoreId;

            //reset checkout data
            if (resetCheckoutData)
            {
                await _customerService.ResetCheckoutData(customer, shoppingCartItem.StoreId);
            }

            //delete item
            customer.ShoppingCartItems.Remove(customer.ShoppingCartItems.Where(x => x.Id == shoppingCartItem.Id).FirstOrDefault());
            await _customerService.DeleteShoppingCartItem(customer.Id, shoppingCartItem);

            //validate checkout attributes
            if (ensureOnlyActiveCheckoutAttributes &&
                //only for shopping cart items (ignore wishlist)
                shoppingCartItem.ShoppingCartTypeId == ShoppingCartType.ShoppingCart)
            {
                var cart = customer.ShoppingCartItems
                    .Where(x => x.ShoppingCartTypeId == ShoppingCartType.ShoppingCart)
                    .LimitPerStore(_shoppingCartSettings.SharedCartBetweenStores, storeId)
                    .ToList();

                var checkoutAttributes = await customer.GetUserField<List<CustomAttribute>>(_userFieldService, SystemCustomerFieldNames.CheckoutAttributes, storeId);
                var newcheckoutAttributes = await _checkoutAttributeParser.EnsureOnlyActiveAttributes(checkoutAttributes, cart);
                await _userFieldService.SaveField(customer, SystemCustomerFieldNames.CheckoutAttributes, newcheckoutAttributes, storeId);
            }

            //event notification
            await _mediator.EntityDeleted(shoppingCartItem);
        }


        /// <summary>
        /// Migrate shopping cart
        /// </summary>
        /// <param name="fromCustomer">From customer</param>
        /// <param name="toCustomer">To customer</param>
        /// <param name="includeCouponCodes">A value indicating whether to coupon codes (discount and gift voucher) should be also re-applied</param>
        public virtual async Task MigrateShoppingCart(Customer fromCustomer, Customer toCustomer, bool includeCouponCodes)
        {
            if (fromCustomer == null)
                throw new ArgumentNullException(nameof(fromCustomer));
            if (toCustomer == null)
                throw new ArgumentNullException(nameof(toCustomer));

            if (fromCustomer.Id == toCustomer.Id)
                return; //the same customer

            //shopping cart items
            var fromCart = fromCustomer.ShoppingCartItems.ToList();
            for (int i = 0; i < fromCart.Count; i++)
            {
                var sci = fromCart[i];
                await AddToCart(toCustomer, sci.ProductId, sci.ShoppingCartTypeId, sci.StoreId, sci.WarehouseId,
                    sci.Attributes, sci.EnteredPrice,
                    sci.RentalStartDateUtc, sci.RentalEndDateUtc, sci.Quantity, false, sci.ReservationId, sci.Parameter, sci.Duration);
            }
            for (int i = 0; i < fromCart.Count; i++)
            {
                var sci = fromCart[i];
                await DeleteShoppingCartItem(fromCustomer, sci);
            }

            //copy discount and gift voucher coupon codes
            if (includeCouponCodes)
            {
                //discount
                var coupons = fromCustomer.ParseAppliedCouponCodes(SystemCustomerFieldNames.DiscountCoupons);
                var resultcoupons = toCustomer.ApplyCouponCode(SystemCustomerFieldNames.DiscountCoupons, coupons);
                await _userFieldService.SaveField(toCustomer, SystemCustomerFieldNames.DiscountCoupons, resultcoupons);

                //gift voucher
                var giftvoucher = fromCustomer.ParseAppliedCouponCodes(SystemCustomerFieldNames.GiftVoucherCoupons);
                var resultgift = toCustomer.ApplyCouponCode(SystemCustomerFieldNames.GiftVoucherCoupons, giftvoucher);
                await _userFieldService.SaveField(toCustomer, SystemCustomerFieldNames.GiftVoucherCoupons, resultgift);
            }

            //copy url referer
            var lastUrlReferrer = await fromCustomer.GetUserField<string>(_userFieldService, SystemCustomerFieldNames.LastUrlReferrer);
            await _userFieldService.SaveField(toCustomer, SystemCustomerFieldNames.LastUrlReferrer, lastUrlReferrer);

            //move selected checkout attributes
            var checkoutAttributes = await fromCustomer.GetUserField<List<CustomAttribute>>(_userFieldService, SystemCustomerFieldNames.CheckoutAttributes, _workContext.CurrentStore.Id);
            await _userFieldService.SaveField(toCustomer, SystemCustomerFieldNames.CheckoutAttributes, checkoutAttributes, _workContext.CurrentStore.Id);
        }

        #endregion
    }
}
