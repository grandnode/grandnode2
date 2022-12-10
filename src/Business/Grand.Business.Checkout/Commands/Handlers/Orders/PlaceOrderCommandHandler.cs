﻿using Grand.Business.Core.Commands.Checkout.Orders;
using Grand.Business.Core.Events.Checkout.Orders;
using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Catalog.Discounts;
using Grand.Business.Core.Interfaces.Catalog.Prices;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Catalog.Tax;
using Grand.Business.Core.Interfaces.Checkout.CheckoutAttributes;
using Grand.Business.Core.Interfaces.Checkout.GiftVouchers;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Payments;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Logging;
using Grand.Business.Core.Interfaces.Common.Pdf;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Interfaces.Messages;
using Grand.Business.Core.Queries.Checkout.Orders;
using Grand.Business.Core.Utilities.Catalog;
using Grand.Business.Core.Utilities.Checkout;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Discounts;
using Grand.Domain.Localization;
using Grand.Domain.Orders;
using Grand.Domain.Payments;
using Grand.Domain.Shipping;
using Grand.Domain.Tax;
using Grand.Infrastructure;
using Grand.SharedKernel;
using Grand.SharedKernel.Extensions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Grand.Business.Checkout.Commands.Handlers.Orders
{
    public class PlaceOrderCommandHandler : IRequestHandler<PlaceOrderCommand, PlaceOrderResult>
    {
        private readonly IOrderService _orderService;
        private readonly ITranslationService _translationService;
        private readonly ILanguageService _languageService;
        private readonly IProductService _productService;
        private readonly IInventoryManageService _inventoryManageService;
        private readonly IPaymentService _paymentService;
        private readonly IPaymentTransactionService _paymentTransactionService;
        private readonly ILogger _logger;
        private readonly IOrderCalculationService _orderTotalCalculationService;
        private readonly IPricingService _pricingService;
        private readonly IPriceFormatter _priceFormatter;
        private readonly IProductAttributeFormatter _productAttributeFormatter;
        private readonly IGiftVoucherService _giftVoucherService;
        private readonly ICheckoutAttributeFormatter _checkoutAttributeFormatter;
        private readonly ITaxService _taxService;
        private readonly ICustomerService _customerService;
        private readonly IDiscountService _discountService;
        private readonly IWorkContext _workContext;
        private readonly IGroupService _groupService;
        private readonly IMessageProviderService _messageProviderService;
        private readonly IVendorService _vendorService;
        private readonly ISalesEmployeeService _salesEmployeeService;
        private readonly ICurrencyService _currencyService;
        private readonly IAffiliateService _affiliateService;
        private readonly IMediator _mediator;
        private readonly IProductReservationService _productReservationService;
        private readonly IAuctionService _auctionService;
        private readonly ICountryService _countryService;
        private readonly IShoppingCartValidator _shoppingCartValidator;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ShippingSettings _shippingSettings;
        private readonly ShoppingCartSettings _shoppingCartSettings;
        private readonly PaymentSettings _paymentSettings;
        private readonly OrderSettings _orderSettings;
        private readonly TaxSettings _taxSettings;

        public PlaceOrderCommandHandler(
            IOrderService orderService,
            ITranslationService translationService,
            ILanguageService languageService,
            IProductService productService,
            IInventoryManageService inventoryManageService,
            IPaymentService paymentService,
            IPaymentTransactionService paymentTransactionService,
            ILogger logger,
            IOrderCalculationService orderTotalCalculationService,
            IPricingService priceCalculationService,
            IPriceFormatter priceFormatter,
            IProductAttributeFormatter productAttributeFormatter,
            IGiftVoucherService giftVoucherService,
            ICheckoutAttributeFormatter checkoutAttributeFormatter,
            ITaxService taxService,
            ICustomerService customerService,
            IDiscountService discountService,
            IWorkContext workContext,
            IGroupService groupService,
            IMessageProviderService messageProviderService,
            IVendorService vendorService,
            ISalesEmployeeService salesEmployeeService,
            ICurrencyService currencyService,
            IAffiliateService affiliateService,
            IMediator mediator,
            IProductReservationService productReservationService,
            IAuctionService auctionService,
            ICountryService countryService,
            IShoppingCartValidator shoppingCartValidator,
            IServiceScopeFactory serviceScopeFactory,
            ShippingSettings shippingSettings,
            ShoppingCartSettings shoppingCartSettings,
            PaymentSettings paymentSettings,
            OrderSettings orderSettings,
            TaxSettings taxSettings)
        {
            _orderService = orderService;
            _translationService = translationService;
            _languageService = languageService;
            _productService = productService;
            _inventoryManageService = inventoryManageService;
            _paymentService = paymentService;
            _paymentTransactionService = paymentTransactionService;
            _logger = logger;
            _orderTotalCalculationService = orderTotalCalculationService;
            _pricingService = priceCalculationService;
            _priceFormatter = priceFormatter;
            _productAttributeFormatter = productAttributeFormatter;
            _giftVoucherService = giftVoucherService;
            _checkoutAttributeFormatter = checkoutAttributeFormatter;
            _taxService = taxService;
            _customerService = customerService;
            _groupService = groupService;
            _discountService = discountService;
            _workContext = workContext;
            _messageProviderService = messageProviderService;
            _vendorService = vendorService;
            _salesEmployeeService = salesEmployeeService;
            _currencyService = currencyService;
            _affiliateService = affiliateService;
            _mediator = mediator;
            _productReservationService = productReservationService;
            _auctionService = auctionService;
            _countryService = countryService;
            _shoppingCartValidator = shoppingCartValidator;
            _serviceScopeFactory = serviceScopeFactory;
            _shippingSettings = shippingSettings;
            _shoppingCartSettings = shoppingCartSettings;
            _paymentSettings = paymentSettings;
            _orderSettings = orderSettings;
            _taxSettings = taxSettings;
        }

        public async Task<PlaceOrderResult> Handle(PlaceOrderCommand command, CancellationToken cancellationToken)
        {
            var result = new PlaceOrderResult();
            try
            {
                //prepare order details
                var details = await PreparePlaceOrderDetails();

                //event notification
                await _mediator.PlaceOrderDetailsEvent(result, details);

                //return if exist errors
                if (result.Errors.Any())
                    return result;

                #region Payment workflow

                var processPayment = await PrepareProcessPayment(details);

                #endregion

                if (processPayment.paymentResult.Success)
                {
                    #region Save order details

                    var orderHeader = PrepareOrderHeader(processPayment.paymentTransaction, processPayment.paymentResult, details);

                    result.PlacedOrder = await SaveOrderDetails(details, orderHeader);
                    result.PaymentTransaction = processPayment.paymentTransaction;

                    #endregion

                    #region Payment transaction

                    await UpdatePaymentTransaction(processPayment.paymentTransaction, result.PlacedOrder, processPayment.paymentResult);

                    #endregion

                    #region Events & notes

                    _ = Task.Run(() => SendNotification(_serviceScopeFactory, result.PlacedOrder, _workContext.CurrentCustomer, _workContext.OriginalCustomerIfImpersonated), cancellationToken);

                    //check order status
                    await _mediator.Send(new CheckOrderStatusCommand { Order = result.PlacedOrder }, cancellationToken);

                    //raise event       
                    await _mediator.Publish(new OrderPlacedEvent(result.PlacedOrder), cancellationToken);

                    if (result.PlacedOrder.PaymentStatusId == PaymentStatus.Paid)
                    {
                        await _mediator.Send(new ProcessOrderPaidCommand { Order = result.PlacedOrder }, cancellationToken);
                    }

                    #endregion
                }
                else
                {
                    await _paymentTransactionService.SetError(processPayment.paymentTransaction.Id, processPayment.paymentResult.Errors.ToList());
                    foreach (var paymentError in processPayment.paymentResult.Errors)
                        result.AddError(string.Format(_translationService.GetResource("Checkout.PaymentError"), paymentError));
                }
            }
            catch (Exception exc)
            {
                _ = _logger.Error(exc.Message, exc);
                result.AddError(exc.Message);
            }

            #region Process errors

            var error = "";
            for (var i = 0; i < result.Errors.Count; i++)
            {
                error += $"Error {i + 1}: {result.Errors[i]}";
                if (i != result.Errors.Count - 1)
                    error += ". ";
            }

            if (string.IsNullOrEmpty(error)) return result;
            //log it
            var logError = $"Error while placing order. {error}";
            _ = _logger.Error(logError);

            #endregion

            return result;
        }

        protected virtual async Task<(ProcessPaymentResult paymentResult, PaymentTransaction paymentTransaction)>
           PrepareProcessPayment(PlaceOrderContainer details)
        {
            //payment transaction
            var paymentTransaction = await PreparePaymentTransaction(details);

            //process payment
            ProcessPaymentResult processPaymentResult;
            //skip payment workflow if order total equals zero
            var skipPaymentWorkflow = details.OrderTotal == 0;
            if (!skipPaymentWorkflow)
            {
                var paymentMethod = _paymentService.LoadPaymentMethodBySystemName(details.PaymentMethodSystemName);
                if (paymentMethod == null)
                    throw new GrandException("Payment method couldn't be loaded");

                //ensure that payment method is active
                if (!paymentMethod.IsPaymentMethodActive(_paymentSettings))
                    throw new GrandException("Payment method is not active");

                //standard cart
                processPaymentResult = await _paymentService.ProcessPayment(paymentTransaction);
            }
            else
            {
                //payment is not required
                processPaymentResult = new ProcessPaymentResult {
                    NewPaymentTransactionStatus = TransactionStatus.Paid
                };
            }

            if (processPaymentResult == null)
                throw new GrandException("processPaymentResult is not available");

            return (processPaymentResult, paymentTransaction);
        }

        private async Task<PaymentTransaction> PreparePaymentTransaction(PlaceOrderContainer details)
        {
            var update = false;
            PaymentTransaction paymentTransaction = new PaymentTransaction { CreatedOnUtc = DateTime.UtcNow };
            var paymentTransactionId = details.Customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.PaymentTransaction, _workContext.CurrentStore.Id);
            if (!string.IsNullOrEmpty(paymentTransactionId))
            {
                paymentTransaction = await _paymentTransactionService.GetById(paymentTransactionId);
                if (paymentTransaction != null)
                {
                    update = true;
                    paymentTransaction.UpdatedOnUtc = DateTime.UtcNow;
                }
                else
                     paymentTransaction = new PaymentTransaction { CreatedOnUtc = DateTime.UtcNow };
            }

            paymentTransaction.TransactionStatus = TransactionStatus.Pending;
            paymentTransaction.IPAddress = details.Customer.LastIpAddress;
            paymentTransaction.OrderCode = string.IsNullOrEmpty(paymentTransaction.OrderCode) ? await _mediator.Send(new PrepareOrderCodeCommand()) : paymentTransaction.OrderCode;
            paymentTransaction.OrderGuid = paymentTransaction.OrderGuid == Guid.Empty ? Guid.NewGuid() : paymentTransaction.OrderGuid;
            paymentTransaction.PaymentMethodSystemName = details.PaymentMethodSystemName;
            paymentTransaction.StoreId = _workContext.CurrentStore.Id;
            paymentTransaction.CustomerId = details.Customer.Id;
            paymentTransaction.TransactionAmount = details.OrderTotal;
            paymentTransaction.CurrencyRate = details.CurrencyRate;
            paymentTransaction.CurrencyCode = details.Currency.CurrencyCode;
            paymentTransaction.CustomerEmail = details.Customer.BillingAddress.Email;
            paymentTransaction.Temp = true;

            if (update)
                await _paymentTransactionService.UpdatePaymentTransaction(paymentTransaction);
            else
                await _paymentTransactionService.InsertPaymentTransaction(paymentTransaction);

            return paymentTransaction;
        }

        private async Task UpdatePaymentTransaction(PaymentTransaction paymentTransaction, Order order, ProcessPaymentResult processPaymentResult)
        {
            paymentTransaction.TransactionAmount = order.OrderTotal;
            paymentTransaction.PaidAmount = order.PaidAmount;
            paymentTransaction.Temp = false;
            paymentTransaction.TransactionStatus = processPaymentResult.NewPaymentTransactionStatus;
            paymentTransaction.Errors.Clear();
            await _paymentTransactionService.UpdatePaymentTransaction(paymentTransaction);
        }
        private async Task<double?> PrepareCommissionRate(Product product, PlaceOrderContainer details)
        {
            var commissionRate = default(double?);
            if (!string.IsNullOrEmpty(product.VendorId))
            {
                var vendor = await _vendorService.GetVendorById(product.VendorId);
                if (vendor is { Commission: { } })
                    commissionRate = vendor.Commission.Value;
            }

            if (commissionRate.HasValue) return commissionRate;
            if (string.IsNullOrEmpty(details.Customer.SeId)) return null;
            var salesEmployee = await _salesEmployeeService.GetSalesEmployeeById(details.Customer.SeId);
            if (salesEmployee is { Active: true, Commission: { } })
                commissionRate = salesEmployee.Commission.Value;

            return commissionRate;
        }

        /// <summary>
        /// Gets shopping cart item weight (of one item)
        /// </summary>
        /// <param name="shoppingCartItem">Shopping cart item</param>
        /// <returns>Shopping cart item weight</returns>
        private async Task<double> GetShoppingCartItemWeight(ShoppingCartItem shoppingCartItem)
        {
            if (shoppingCartItem == null)
                throw new ArgumentNullException(nameof(shoppingCartItem));
            var product = await _productService.GetProductById(shoppingCartItem.ProductId);
            if (product == null)
                return 0;

            //attribute weight
            double attributesTotalWeight = 0;
            if (shoppingCartItem.Attributes != null && shoppingCartItem.Attributes.Any())
            {
                var attributeValues = product.ParseProductAttributeValues(shoppingCartItem.Attributes);
                foreach (var attributeValue in attributeValues)
                {
                    switch (attributeValue.AttributeValueTypeId)
                    {
                        case AttributeValueType.Simple:
                            {
                                //simple attribute
                                attributesTotalWeight += attributeValue.WeightAdjustment;
                            }
                            break;
                        case AttributeValueType.AssociatedToProduct:
                            {
                                //bundled product
                                var associatedProduct = await _productService.GetProductById(attributeValue.AssociatedProductId);
                                if (associatedProduct is { IsShipEnabled: true })
                                {
                                    attributesTotalWeight += associatedProduct.Weight * attributeValue.Quantity;
                                }
                            }
                            break;
                        default:
                            break;
                    }
                }
            }
            var weight = product.Weight + attributesTotalWeight;
            return weight;
        }

        protected virtual async Task<PlaceOrderContainer> PreparePlaceOrderDetails()
        {
            var details = new PlaceOrderContainer {
                //customer
                Customer = _workContext.CurrentCustomer
            };
            if (details.Customer == null)
                throw new ArgumentException("Customer is not set");

            //check whether customer is guest
            if (await _groupService.IsGuest(details.Customer) && !_orderSettings.AnonymousCheckoutAllowed)
                throw new GrandException("Anonymous checkout is not allowed");

            //billing address
            if (details.Customer.BillingAddress == null)
                throw new GrandException("Billing address is not provided");

            if (!CommonHelper.IsValidEmail(details.Customer.BillingAddress.Email))
                throw new GrandException("Email is not valid");

            //affiliate
            if (!string.IsNullOrEmpty(details.Customer.AffiliateId))
            {
                var affiliate = await _affiliateService.GetAffiliateById(details.Customer.AffiliateId);
                if (affiliate is { Active: true })
                    details.AffiliateId = affiliate.Id;
            }

            //customer currency
            var currencyTmp = await _currencyService.GetCurrencyById(details.Customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.CurrencyId, _workContext.CurrentStore.Id));
            var customerCurrency = currencyTmp is { Published: true } ? currencyTmp : _workContext.WorkingCurrency;
            details.Currency = customerCurrency;
            var primaryStoreCurrency = await _currencyService.GetPrimaryStoreCurrency();
            details.CurrencyRate = Math.Round(customerCurrency.Rate / primaryStoreCurrency.Rate, 6);
            details.PrimaryCurrencyCode = primaryStoreCurrency.CurrencyCode;

            //customer language
            details.Language = await _languageService.GetLanguageById(details.Customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.LanguageId, _workContext.CurrentStore.Id));

            if (details.Language is not { Published: true })
                details.Language = _workContext.WorkingLanguage;

            details.BillingAddress = details.Customer.BillingAddress;
            if (!string.IsNullOrEmpty(details.BillingAddress.CountryId))
            {
                var country = await _countryService.GetCountryById(details.BillingAddress.CountryId);
                if (country is { AllowsBilling: false }) throw new GrandException($"Country '{country.Name}' is not allowed for billing");
            }

            //checkout attributes
            details.CheckoutAttributes = details.Customer.GetUserFieldFromEntity<List<CustomAttribute>>(SystemCustomerFieldNames.CheckoutAttributes, _workContext.CurrentStore.Id);
            details.CheckoutAttributeDescription = await _checkoutAttributeFormatter.FormatAttributes(details.CheckoutAttributes, details.Customer);

            //load and validate customer shopping cart
            details.Cart = details.Customer.ShoppingCartItems
                .Where(sci => sci.ShoppingCartTypeId is ShoppingCartType.ShoppingCart or ShoppingCartType.Auctions)
                .LimitPerStore(_shoppingCartSettings.SharedCartBetweenStores, _workContext.CurrentStore.Id)
                .ToList();

            if (!details.Cart.Any())
                throw new GrandException("Cart is empty");

            //validate the entire shopping cart
            var warnings = await _shoppingCartValidator.GetShoppingCartWarnings(details.Cart, details.CheckoutAttributes, true);
            if (warnings.Any())
            {
                var warningsSb = new StringBuilder();
                foreach (var warning in warnings)
                {
                    warningsSb.Append(warning);
                    warningsSb.Append(';');
                }
                throw new GrandException(warningsSb.ToString());
            }

            //validate individual cart items
            foreach (var sci in details.Cart)
            {
                var product = await _productService.GetProductById(sci.ProductId);
                var sciWarnings = await _shoppingCartValidator.GetShoppingCartItemWarnings(details.Customer, sci, product, new ShoppingCartValidatorOptions());
                if (sciWarnings.Any())
                {
                    var warningsSb = new StringBuilder();
                    foreach (var warning in sciWarnings)
                    {
                        warningsSb.Append(warning);
                        warningsSb.Append(';');
                    }
                    throw new GrandException(warningsSb.ToString());
                }

                if (!product.IsRecurring) continue;
                details.IsRecurring = true;
                details.RecurringCycleLength = product.RecurringCycleLength;
                details.RecurringCyclePeriodId = product.RecurringCyclePeriodId;
                details.RecurringTotalCycles = product.RecurringTotalCycles;
            }

            //min totals validation
            var minOrderSubtotalAmountOk = await _mediator.Send(new ValidateMinShoppingCartSubtotalAmountCommand() { Customer = _workContext.CurrentCustomer, Cart = details.Cart });
            if (!minOrderSubtotalAmountOk)
            {
                var minOrderSubtotalAmount = await _currencyService.ConvertFromPrimaryStoreCurrency(_orderSettings.MinOrderSubtotalAmount, _workContext.WorkingCurrency);
                throw new GrandException(string.Format(_translationService.GetResource("Checkout.MinOrderSubtotalAmount"), _priceFormatter.FormatPrice(minOrderSubtotalAmount, false)));
            }

            var minmaxOrderTotalAmountOk = await _mediator.Send(new ValidateShoppingCartTotalAmountCommand() { Customer = details.Customer, Cart = details.Cart });
            if (!minmaxOrderTotalAmountOk)
            {
                throw new GrandException(_translationService.GetResource("Checkout.MinMaxOrderTotalAmount"));
            }

            //tax display type
            if (_taxSettings.AllowCustomersToSelectTaxDisplayType)
                details.TaxDisplayType = (TaxDisplayType)details.Customer.GetUserFieldFromEntity<int>(SystemCustomerFieldNames.TaxDisplayTypeId, _workContext.CurrentStore.Id);
            else
                details.TaxDisplayType = _taxSettings.TaxDisplayType;

            //sub total
            //sub total (incl tax)
            var shoppingCartSubTotal = await _orderTotalCalculationService.GetShoppingCartSubTotal(details.Cart, true);
            var orderSubTotalDiscountAmount = shoppingCartSubTotal.discountAmount;
            List<ApplyDiscount> orderSubTotalAppliedDiscounts = shoppingCartSubTotal.appliedDiscounts;
            var subTotalWithoutDiscountBase = shoppingCartSubTotal.subTotalWithoutDiscount;

            details.OrderSubTotalInclTax = subTotalWithoutDiscountBase;
            details.OrderSubTotalDiscountInclTax = orderSubTotalDiscountAmount;

            foreach (var disc in orderSubTotalAppliedDiscounts.Where(disc => details.AppliedDiscounts.All(x => x.DiscountId != disc.DiscountId)))
                details.AppliedDiscounts.Add(disc);

            //sub total (excl tax)
            shoppingCartSubTotal = await _orderTotalCalculationService.GetShoppingCartSubTotal(details.Cart, false);
            orderSubTotalDiscountAmount = shoppingCartSubTotal.discountAmount;
            subTotalWithoutDiscountBase = shoppingCartSubTotal.subTotalWithoutDiscount;

            details.OrderSubTotalExclTax = subTotalWithoutDiscountBase;
            details.OrderSubTotalDiscountExclTax = orderSubTotalDiscountAmount;

            //shipping info
            var shoppingCartRequiresShipping = details.Cart.RequiresShipping();

            if (shoppingCartRequiresShipping)
            {
                var pickupPoint = details.Customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.SelectedPickupPoint, _workContext.CurrentStore.Id);
                if (_shippingSettings.AllowPickUpInStore && pickupPoint != null)
                {
                    details.PickUpInStore = true;
                    details.PickupPoint = await _mediator.Send(new GetPickupPointById { Id = pickupPoint });
                }
                else
                {
                    if (details.Customer.ShippingAddress == null)
                        throw new GrandException("Shipping address is not provided");

                    if (!CommonHelper.IsValidEmail(details.Customer.ShippingAddress.Email))
                        throw new GrandException("Email is not valid");

                    //clone shipping address
                    details.ShippingAddress = details.Customer.ShippingAddress;
                    if (!string.IsNullOrEmpty(details.ShippingAddress.CountryId))
                    {
                        var country = await _countryService.GetCountryById(details.ShippingAddress.CountryId);
                        if (country is { AllowsShipping: false }) throw new GrandException($"Country '{country.Name}' is not allowed for shipping");
                    }
                }
                var shippingOption = details.Customer.GetUserFieldFromEntity<ShippingOption>(SystemCustomerFieldNames.SelectedShippingOption, _workContext.CurrentStore.Id);
                if (shippingOption != null)
                {
                    details.ShippingMethodName = shippingOption.Name;
                    details.ShippingRateProviderSystemName = shippingOption.ShippingRateProviderSystemName;
                }
            }
            details.ShippingStatus = shoppingCartRequiresShipping
                ? ShippingStatus.Pending
                : ShippingStatus.ShippingNotRequired;

            //shipping total

            var (orderShippingTotalInclTax, _, shippingTotalDiscounts) = await _orderTotalCalculationService.GetShoppingCartShippingTotal(details.Cart, true);
            var orderShippingTotalExclTax = (await _orderTotalCalculationService.GetShoppingCartShippingTotal(details.Cart, false)).shoppingCartShippingTotal;
            if (!orderShippingTotalInclTax.HasValue || !orderShippingTotalExclTax.HasValue)
                throw new GrandException("Shipping total couldn't be calculated");

            foreach (var disc in shippingTotalDiscounts.Where(disc => details.AppliedDiscounts.All(x => x.DiscountId != disc.DiscountId)))
            {
                details.AppliedDiscounts.Add(disc);
            }
            details.OrderShippingTotalInclTax = orderShippingTotalInclTax.Value;
            details.OrderShippingTotalExclTax = orderShippingTotalExclTax.Value;

            //payment 
            var paymentMethodSystemName = _workContext.CurrentCustomer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.SelectedPaymentMethod, _workContext.CurrentStore.Id);
            details.PaymentMethodSystemName = paymentMethodSystemName;
            var paymentAdditionalFee = await _paymentService.GetAdditionalHandlingFee(details.Cart, paymentMethodSystemName);
            details.PaymentAdditionalFeeInclTax = (await _taxService.GetPaymentMethodAdditionalFee(paymentAdditionalFee, true, details.Customer)).paymentPrice;
            details.PaymentAdditionalFeeExclTax = (await _taxService.GetPaymentMethodAdditionalFee(paymentAdditionalFee, false, details.Customer)).paymentPrice;

            //tax total
            //tax amount
            var (taxTotal, taxRates) = await _orderTotalCalculationService.GetTaxTotal(details.Cart);
            details.OrderTaxTotal = taxTotal;

            //tax rates
            foreach (var kvp in taxRates)
            {
                details.Taxes.Add(new OrderTax {
                    Percent = Math.Round(kvp.Key, 2),
                    Amount = kvp.Value
                });
            }

            //order total (and applied discounts, gift vouchers, loyalty points)
            var (orderTotal, orderDiscountAmount, orderAppliedDiscounts, appliedGiftVouchers, redeemedLoyaltyPoints, redeemedLoyaltyPointsAmount) = await _orderTotalCalculationService.GetShoppingCartTotal(details.Cart);
            if (!orderTotal.HasValue)
                throw new GrandException("Order total couldn't be calculated");

            details.OrderDiscountAmount = orderDiscountAmount;
            details.RedeemedLoyaltyPoints = redeemedLoyaltyPoints;
            details.RedeemedLoyaltyPointsAmount = redeemedLoyaltyPointsAmount;
            details.AppliedGiftVouchers = appliedGiftVouchers;
            details.OrderTotal = orderTotal.Value;

            //discount history
            foreach (var disc in orderAppliedDiscounts.Where(disc => details.AppliedDiscounts.All(x => x.DiscountId != disc.DiscountId)))
            {
                details.AppliedDiscounts.Add(disc);
            }

            return details;
        }

        protected virtual async Task<OrderItem> PrepareOrderItem(ShoppingCartItem sc, Product product, PlaceOrderContainer details)
        {
            var scUnitPrice = (await _pricingService.GetUnitPrice(sc, product)).unitprice;
            var scUnitPriceWithoutDisc = (await _pricingService.GetUnitPrice(sc, product, false)).unitprice;

            var (scSubTotal, discountAmount, scDiscounts) = await _pricingService.GetSubTotal(sc, product);

            var prices = await _taxService.GetTaxProductPrice(product, details.Customer, scUnitPrice, scUnitPriceWithoutDisc, sc.Quantity, scSubTotal, discountAmount, _taxSettings.PricesIncludeTax);
            var scUnitPriceWithoutDiscInclTax = prices.UnitPriceWithoutDiscInclTax;
            var scUnitPriceWithoutDiscExclTax = prices.UnitPriceWithoutDiscExclTax;
            var scUnitPriceInclTax = prices.UnitPriceInclTax;
            var scUnitPriceExclTax = prices.UnitPriceExclTax;
            var scSubTotalInclTax = prices.SubTotalInclTax;
            var scSubTotalExclTax = prices.SubTotalExclTax;
            var discountAmountInclTax = prices.DiscountAmountInclTax;
            var discountAmountExclTax = prices.DiscountAmountExclTax;

            foreach (var disc in scDiscounts.Where(disc => details.AppliedDiscounts.All(x => x.DiscountId != disc.DiscountId)))
            {
                details.AppliedDiscounts.Add(disc);
            }

            //attributes
            var attributeDescription = await _productAttributeFormatter.FormatAttributes(product, sc.Attributes, details.Customer);

            if (string.IsNullOrEmpty(attributeDescription) && sc.ShoppingCartTypeId == ShoppingCartType.Auctions)
                attributeDescription = _translationService.GetResource("ShoppingCart.AuctionWonOn") + " " + product.AvailableEndDateTimeUtc;

            var itemWeight = await GetShoppingCartItemWeight(sc);

            var warehouseId = !string.IsNullOrEmpty(sc.WarehouseId) ? sc.WarehouseId : _workContext.CurrentStore.DefaultWarehouseId;
            if (!product.UseMultipleWarehouses && string.IsNullOrEmpty(warehouseId))
            {
                if (!string.IsNullOrEmpty(product.WarehouseId))
                {
                    warehouseId = product.WarehouseId;
                }
            }

            var commissionRate = await PrepareCommissionRate(product, details);
            var commission = commissionRate.HasValue ?
                Math.Round(commissionRate.Value * scSubTotal / 100, 2) : 0;

            //save order item
            var orderItem = new OrderItem {
                OrderItemGuid = Guid.NewGuid(),
                ProductId = sc.ProductId,
                Sku = product.FormatSku(sc.Attributes),
                VendorId = product.VendorId,
                WarehouseId = warehouseId,
                SeId = details.Customer.SeId,
                TaxRate = Math.Round(prices.TaxRate, 2),
                UnitPriceWithoutDiscInclTax = Math.Round(scUnitPriceWithoutDiscInclTax, 6),
                UnitPriceWithoutDiscExclTax = Math.Round(scUnitPriceWithoutDiscExclTax, 6),
                UnitPriceInclTax = Math.Round(scUnitPriceInclTax, 6),
                UnitPriceExclTax = Math.Round(scUnitPriceExclTax, 6),
                PriceInclTax = Math.Round(scSubTotalInclTax, 6),
                PriceExclTax = Math.Round(scSubTotalExclTax, 6),
                OriginalProductCost = await _pricingService.GetProductCost(product, sc.Attributes),
                AttributeDescription = attributeDescription,
                Attributes = sc.Attributes,
                Quantity = sc.Quantity,
                OpenQty = sc.Quantity,
                IsShipEnabled = product.IsShipEnabled,
                DiscountAmountInclTax = Math.Round(discountAmountInclTax, 6),
                DiscountAmountExclTax = Math.Round(discountAmountExclTax, 6),
                DownloadCount = 0,
                IsDownloadActivated = false,
                LicenseDownloadId = "",
                ItemWeight = itemWeight,
                RentalStartDateUtc = sc.RentalStartDateUtc,
                RentalEndDateUtc = sc.RentalEndDateUtc,
                CreatedOnUtc = DateTime.UtcNow,
                Commission = commission,
                cId = sc.cId
            };

            var reservationInfo = "";
            if (product.ProductTypeId == ProductType.Reservation)
            {
                if (sc.RentalEndDateUtc == default(DateTime) || sc.RentalEndDateUtc == null)
                {
                    reservationInfo = sc.RentalStartDateUtc.ToString();
                }
                else
                {
                    reservationInfo = sc.RentalStartDateUtc + " - " + sc.RentalEndDateUtc;
                }
                if (!string.IsNullOrEmpty(sc.Parameter))
                {
                    reservationInfo += "<br>" + string.Format(_translationService.GetResource("ShoppingCart.Reservation.Option"), sc.Parameter);
                }
                if (!string.IsNullOrEmpty(sc.Duration))
                {
                    reservationInfo += "<br>" + _translationService.GetResource("Products.Duration") + ": " + sc.Duration;
                }
            }

            if (string.IsNullOrEmpty(reservationInfo)) return orderItem;
            if (!string.IsNullOrEmpty(orderItem.AttributeDescription))
            {
                orderItem.AttributeDescription += "<br>" + reservationInfo;
            }
            else
            {
                orderItem.AttributeDescription = reservationInfo;
            }
            return orderItem;
        }

        protected virtual async Task GenerateGiftVoucher(PlaceOrderContainer details, ShoppingCartItem sc, Order order, OrderItem orderItem, Product product)
        {
            GiftVoucherExtensions.GetGiftVoucherAttribute(sc.Attributes,
                        out var giftVoucherRecipientName, out var giftVoucherRecipientEmail,
                        out var giftVoucherSenderName, out var giftVoucherSenderEmail, out var giftVoucherMessage);

            for (var i = 0; i < sc.Quantity; i++)
            {
                var amount = orderItem.UnitPriceInclTax;
                if (product.OverGiftAmount.HasValue)
                    amount = await _currencyService.ConvertFromPrimaryStoreCurrency(product.OverGiftAmount.Value, details.Currency);

                var gc = new GiftVoucher {
                    GiftVoucherTypeId = product.GiftVoucherTypeId,
                    PurchasedWithOrderItem = orderItem,
                    Amount = amount,
                    CurrencyCode = order.CustomerCurrencyCode,
                    IsGiftVoucherActivated = false,
                    Code = _giftVoucherService.GenerateGiftVoucherCode(),
                    RecipientName = giftVoucherRecipientName,
                    RecipientEmail = giftVoucherRecipientEmail,
                    SenderName = giftVoucherSenderName,
                    SenderEmail = giftVoucherSenderEmail,
                    Message = giftVoucherMessage,
                    IsRecipientNotified = false,
                    StoreId = _orderSettings.GiftVouchers_Assign_StoreId ? _workContext.CurrentStore.Id : string.Empty,
                    CreatedOnUtc = DateTime.UtcNow
                };
                await _giftVoucherService.InsertGiftVoucher(gc);
            }
        }

        protected virtual async Task UpdateProductReservation(Order order, PlaceOrderContainer details)
        {
            var reservationsToUpdate = new List<ProductReservation>();
            foreach (var sc in details.Cart.Where(x => (x.RentalStartDateUtc.HasValue && x.RentalEndDateUtc.HasValue) || !string.IsNullOrEmpty(x.ReservationId)))
            {
                var product = await _productService.GetProductById(sc.ProductId);
                if (!string.IsNullOrEmpty(sc.ReservationId))
                {
                    var reservation = await _productReservationService.GetProductReservation(sc.ReservationId);
                    reservationsToUpdate.Add(reservation);
                }

                if (!sc.RentalStartDateUtc.HasValue || !sc.RentalEndDateUtc.HasValue) continue;
                var reservations = await _productReservationService.GetProductReservationsByProductId(product.Id, true, null);
                var grouped = reservations.GroupBy(x => x.Resource);

                IGrouping<string, ProductReservation> groupToBook = null;
                foreach (var group in grouped)
                {
                    var groupCanBeBooked = true;
                    if (product.IncBothDate && product.IntervalUnitId == IntervalUnit.Day)
                    {
                        for (DateTime iterator = sc.RentalStartDateUtc.Value; iterator <= sc.RentalEndDateUtc.Value; iterator += new TimeSpan(24, 0, 0))
                        {
                            if (group.Select(x => x.Date).Contains(iterator)) continue;
                            groupCanBeBooked = false;
                            break;
                        }
                    }
                    else
                    {
                        for (DateTime iterator = sc.RentalStartDateUtc.Value; iterator < sc.RentalEndDateUtc.Value; iterator += new TimeSpan(24, 0, 0))
                        {
                            if (group.Select(x => x.Date).Contains(iterator)) continue;
                            groupCanBeBooked = false;
                            break;
                        }
                    }

                    if (!groupCanBeBooked) continue;
                    groupToBook = group;
                    break;
                }

                if (groupToBook == null)
                {
                    throw new Exception("ShoppingCart.Reservation.NoFreeReservationsInThisPeriod");
                }

                var temp = groupToBook.AsQueryable();
                if (product.IncBothDate && product.IntervalUnitId == IntervalUnit.Day)
                {
                    temp = temp.Where(x => x.Date >= sc.RentalStartDateUtc && x.Date <= sc.RentalEndDateUtc);
                }
                else
                {
                    temp = temp.Where(x => x.Date >= sc.RentalStartDateUtc && x.Date < sc.RentalEndDateUtc);
                }

                foreach (var item in temp)
                {
                    item.OrderId = order.OrderGuid.ToString();
                    await _productReservationService.UpdateProductReservation(item);
                }

                reservationsToUpdate.AddRange(temp);
            }
            var reserved = await _productReservationService.GetCustomerReservationsHelpers(order.CustomerId);
            foreach (var res in reserved)
            {
                await _productReservationService.DeleteCustomerReservationsHelper(res);
            }

            foreach (var resToUpdate in reservationsToUpdate)
            {
                resToUpdate.OrderId = order.Id;
                await _productReservationService.UpdateProductReservation(resToUpdate);
            }

        }

        protected virtual async Task UpdateAuctionEnded(ShoppingCartItem sc, Product product, Order order)
        {
            if (product.ProductTypeId == ProductType.Auction && sc.ShoppingCartTypeId == ShoppingCartType.ShoppingCart)
            {
                await _auctionService.UpdateAuctionEnded(product, true, true);
                await _auctionService.UpdateHighestBid(product, product.Price, order.CustomerId);
                await _messageProviderService.SendAuctionEndedBinCustomerMessage(product, order.CustomerId, order.CustomerLanguageId, order.StoreId);
                await _auctionService.InsertBid(new Bid() {
                    CustomerId = order.CustomerId,
                    OrderId = order.Id,
                    Amount = product.Price,
                    Date = DateTime.UtcNow,
                    ProductId = product.Id,
                    StoreId = order.StoreId,
                    Win = true,
                    Bin = true,
                });
            }
            if (product.ProductTypeId == ProductType.Auction && _orderSettings.UnpublishAuctionProduct)
            {
                await _productService.UnPublishProduct(product);
            }
        }

        protected virtual async Task UpdateBids(Order order, PlaceOrderContainer details)
        {
            foreach (var sc in details.Cart.Where(x => x.ShoppingCartTypeId == ShoppingCartType.Auctions))
            {
                var bid = (await _auctionService.GetBidsByProductId(sc.Id)).FirstOrDefault(x => x.CustomerId == details.Customer.Id);
                if (bid == null) continue;
                bid.OrderId = order.Id;
                await _auctionService.UpdateBid(bid);
            }
        }
        protected virtual async Task InsertDiscountUsageHistory(Order order, PlaceOrderContainer details)
        {
            foreach (var discount in details.AppliedDiscounts)
            {
                var duh = new DiscountUsageHistory {
                    DiscountId = discount.DiscountId,
                    CouponCode = discount.CouponCode,
                    OrderId = order.Id,
                    CustomerId = order.CustomerId,
                    CreatedOnUtc = DateTime.UtcNow
                };
                await _discountService.InsertDiscountUsageHistory(duh);
            }
        }

        protected virtual async Task AppliedGiftVouchers(Order order, PlaceOrderContainer details)
        {
            foreach (var agc in details.AppliedGiftVouchers)
            {
                var amountUsed = agc.AmountCanBeUsed;
                var giftVoucherUsageHistory = new GiftVoucherUsageHistory {
                    GiftVoucherId = agc.GiftVoucher.Id,
                    UsedWithOrderId = order.Id,
                    UsedValue = amountUsed,
                    CreatedOnUtc = DateTime.UtcNow
                };
                agc.GiftVoucher.GiftVoucherUsageHistory.Add(giftVoucherUsageHistory);
                await _giftVoucherService.UpdateGiftVoucher(agc.GiftVoucher);
            }
        }

        /// <summary>
        /// Prepare order header
        /// </summary>
        /// <returns>Order</returns>
        protected virtual Order PrepareOrderHeader(PaymentTransaction paymentTransaction,
            ProcessPaymentResult processPaymentResult, PlaceOrderContainer details)
        {
            var paymentStatus = PaymentStatus.Pending;

            if (processPaymentResult.NewPaymentTransactionStatus == TransactionStatus.Paid && processPaymentResult.PaidAmount == details.OrderTotal)
                paymentStatus = PaymentStatus.Paid;

            if (processPaymentResult.NewPaymentTransactionStatus == TransactionStatus.Paid &&
                processPaymentResult.PaidAmount > 0 &&
                processPaymentResult.PaidAmount < details.OrderTotal)
                paymentStatus = PaymentStatus.PartiallyPaid;


            var order = new Order {
                StoreId = paymentTransaction.StoreId,
                OrderGuid = paymentTransaction.OrderGuid,
                Code = paymentTransaction.OrderCode,
                CustomerId = details.Customer.Id,
                OwnerId = string.IsNullOrEmpty(details.Customer.OwnerId) ? details.Customer.Id : details.Customer.OwnerId,
                SeId = details.Customer.SeId,
                CustomerLanguageId = details.Language.Id,
                CustomerTaxDisplayTypeId = details.TaxDisplayType,
                CustomerIp = details.Customer.LastIpAddress,
                OrderSubtotalInclTax = Math.Round(details.OrderSubTotalInclTax, 6),
                OrderSubtotalExclTax = Math.Round(details.OrderSubTotalExclTax, 6),
                OrderSubTotalDiscountInclTax = Math.Round(details.OrderSubTotalDiscountInclTax, 6),
                OrderSubTotalDiscountExclTax = Math.Round(details.OrderSubTotalDiscountExclTax, 6),
                OrderShippingInclTax = Math.Round(details.OrderShippingTotalInclTax, 6),
                OrderShippingExclTax = Math.Round(details.OrderShippingTotalExclTax, 6),
                PaymentMethodAdditionalFeeInclTax = Math.Round(details.PaymentAdditionalFeeInclTax, 6),
                PaymentMethodAdditionalFeeExclTax = Math.Round(details.PaymentAdditionalFeeExclTax, 6),
                OrderTax = Math.Round(details.OrderTaxTotal, 6),
                OrderTotal = Math.Round(details.OrderTotal, 6),
                RefundedAmount = 0,
                OrderDiscount = Math.Round(details.OrderDiscountAmount, 6),
                CheckoutAttributeDescription = details.CheckoutAttributeDescription,
                CheckoutAttributes = details.CheckoutAttributes,
                CustomerCurrencyCode = details.Currency.CurrencyCode,
                PrimaryCurrencyCode = details.PrimaryCurrencyCode,
                CurrencyRate = details.CurrencyRate,
                Rate = details.Currency.Rate,
                AffiliateId = details.AffiliateId,
                OrderStatusId = (int)OrderStatusSystem.Pending,
                PaymentMethodSystemName = paymentTransaction.PaymentMethodSystemName,
                PaymentOptionAttribute = details.Customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.PaymentOptionAttribute, paymentTransaction.StoreId),
                PaymentStatusId = paymentStatus,
                PaidAmount = processPaymentResult.PaidAmount,
                PaidDateUtc = processPaymentResult.PaidAmount > 0 ? DateTime.UtcNow : null,
                BillingAddress = details.BillingAddress,
                ShippingAddress = details.ShippingAddress,
                ShippingStatusId = details.ShippingStatus,
                ShippingMethod = details.ShippingMethodName,
                PickUpInStore = details.PickUpInStore,
                PickupPoint = details.PickupPoint,
                ShippingRateProviderSystemName = details.ShippingRateProviderSystemName,
                VatNumber = details.Customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.VatNumber),
                VatNumberStatusId = details.Customer.GetUserFieldFromEntity<int>(SystemCustomerFieldNames.VatNumberStatusId),
                CompanyName = details.Customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.Company),
                FirstName = details.Customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.FirstName),
                LastName = details.Customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.LastName),
                CustomerEmail = details.Customer.Email,
                UrlReferrer = details.Customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.LastUrlReferrer),
                ShippingOptionAttributeDescription = details.Customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.ShippingOptionAttributeDescription, paymentTransaction.StoreId),
                ShippingOptionAttribute = details.Customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.ShippingOptionAttribute, paymentTransaction.StoreId),
                RedeemedLoyaltyPoints = details.RedeemedLoyaltyPoints,
                RedeemedLoyaltyPointsAmount = details.RedeemedLoyaltyPointsAmount,
                LoyaltyPointsWereAdded = details.RedeemedLoyaltyPoints > 0,
                IsRecurring = details.IsRecurring,
                RecurringCycleLength = details.RecurringCycleLength,
                RecurringCyclePeriodId = details.RecurringCyclePeriodId,
                RecurringTotalCycles = details.RecurringTotalCycles,
                CreatedOnUtc = DateTime.UtcNow,
            };

            foreach (var item in details.Taxes)
            {
                order.OrderTaxes.Add(item);
            }
            return order;
        }

        /// <summary>
        /// Save order details
        /// </summary>
        /// <param name="details">Place order container</param>
        /// <param name="order">Order</param>
        /// <returns>Order</returns>
        protected virtual async Task<Order> SaveOrderDetails(PlaceOrderContainer details, Order order)
        {
            //move shopping cart items to order items
            foreach (var sc in details.Cart)
            {
                var product = await _productService.GetProductById(sc.ProductId);

                var orderItem = await PrepareOrderItem(sc, product, details);

                order.OrderItems.Add(orderItem);

                //gift vouchers
                if (product.IsGiftVoucher)
                {
                    await GenerateGiftVoucher(details, sc, order, orderItem, product);
                }

                //update auction ended
                await UpdateAuctionEnded(sc, product, order);

                //inventory
                await _inventoryManageService.AdjustReserved(product, -sc.Quantity, sc.Attributes, orderItem.WarehouseId);

                //update sold
                _ = _productService.IncrementProductField(product, x => x.Sold, sc.Quantity);
            }

            //insert order
            await _orderService.InsertOrder(order);

            //update reservation
            await UpdateProductReservation(order, details);

            //update bids
            await UpdateBids(order, details);

            //clear shopping cart
            await _customerService.ClearShoppingCartItem(order.CustomerId, details.Cart);

            //discount usage history
            await InsertDiscountUsageHistory(order, details);

            //gift voucher usage history
            if (details.AppliedGiftVouchers != null)
                await AppliedGiftVouchers(order, details);

            //reset checkout data
            await _customerService.ResetCheckoutData(details.Customer, order.StoreId, clearCouponCodes: true, clearCheckoutAttributes: true);

            return order;
        }

        /// <summary>
        /// Send notification order 
        /// </summary>
        /// <param name="scopeFactory"></param>
        /// <param name="order">Order</param>
        /// <param name="customer"></param>
        /// <param name="originalCustomerIfImpersonated"></param>
        protected virtual async Task SendNotification(IServiceScopeFactory scopeFactory, Order order, Customer customer, Customer originalCustomerIfImpersonated)
        {
            using var scope = scopeFactory.CreateScope();

            var workContext = scope.ServiceProvider.GetService<IWorkContext>();
            await workContext.SetCurrentCustomer(customer);
            await workContext.SetWorkingLanguage(customer);
            await workContext.SetWorkingCurrency(customer);
            await workContext.SetTaxDisplayType(customer);

            var orderService = scope.ServiceProvider.GetRequiredService<IOrderService>();
            var messageProviderService = scope.ServiceProvider.GetRequiredService<IMessageProviderService>();
            var orderSettings = scope.ServiceProvider.GetRequiredService<OrderSettings>();
            var languageSettings = scope.ServiceProvider.GetRequiredService<LanguageSettings>();
            var pdfService = scope.ServiceProvider.GetRequiredService<IPdfService>();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            try
            {
                //notes, messages
                if (originalCustomerIfImpersonated != null)
                {
                    //this order is placed by a store administrator impersonating a customer
                    await orderService.InsertOrderNote(new OrderNote {
                        Note =
                            $"Order placed by a store owner ('{originalCustomerIfImpersonated.Email}'. ID = {originalCustomerIfImpersonated.Id}) impersonating the customer.",
                        DisplayToCustomer = false,
                        CreatedOnUtc = DateTime.UtcNow,
                        OrderId = order.Id,
                    });
                }
                else
                {
                    await orderService.InsertOrderNote(new OrderNote {
                        Note = "Order placed",
                        DisplayToCustomer = false,
                        CreatedOnUtc = DateTime.UtcNow,
                        OrderId = order.Id,

                    });
                }

                //send email notifications
                await messageProviderService.SendOrderPlacedStoreOwnerMessage(order, customer, languageSettings.DefaultAdminLanguageId);

                string orderPlacedAttachmentFilePath = string.Empty, orderPlacedAttachmentFileName = string.Empty;
                var orderPlacedAttachments = new List<string>();

                try
                {
                    orderPlacedAttachmentFilePath = orderSettings.AttachPdfInvoiceToOrderPlacedEmail && !orderSettings.AttachPdfInvoiceToBinary ?
                        await pdfService.PrintOrderToPdf(order, order.CustomerLanguageId) : null;
                    orderPlacedAttachmentFileName = orderSettings.AttachPdfInvoiceToOrderPlacedEmail && !orderSettings.AttachPdfInvoiceToBinary ?
                        "order.pdf" : null;
                    orderPlacedAttachments = orderSettings.AttachPdfInvoiceToOrderPlacedEmail && orderSettings.AttachPdfInvoiceToBinary ?
                        new List<string> { await pdfService.SaveOrderToBinary(order, order.CustomerLanguageId) } : new List<string>();
                }
                catch (Exception ex)
                {
                    _ = _logger.Error($"Error - order placed attachment file {order.OrderNumber}", ex);
                }

                await messageProviderService
                    .SendOrderPlacedCustomerMessage(order, customer, order.CustomerLanguageId, orderPlacedAttachmentFilePath, orderPlacedAttachmentFileName, orderPlacedAttachments);

                if (order.OrderItems.Any(x => !string.IsNullOrEmpty(x.VendorId)))
                {
                    var vendors = await mediator.Send(new GetVendorsInOrderQuery() { Order = order });
                    foreach (var vendor in vendors)
                    {
                        await messageProviderService.SendOrderPlacedVendorMessage(order, customer, vendor, languageSettings.DefaultAdminLanguageId);
                    }
                }
            }
            catch (Exception e)
            {
                await _logger.InsertLog(Domain.Logging.LogLevel.Error, "Place order send notification error", e.Message);
            }

        }
    }
}

