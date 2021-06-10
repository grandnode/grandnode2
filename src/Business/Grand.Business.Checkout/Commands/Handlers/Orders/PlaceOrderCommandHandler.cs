using Grand.Business.Catalog.Extensions;
using Grand.Business.Catalog.Interfaces.Discounts;
using Grand.Business.Catalog.Interfaces.Prices;
using Grand.Business.Catalog.Interfaces.Products;
using Grand.Business.Catalog.Interfaces.Tax;
using Grand.Business.Catalog.Utilities;
using Grand.Business.Checkout.Commands.Models.Orders;
using Grand.Business.Checkout.Events.Orders;
using Grand.Business.Checkout.Extensions;
using Grand.Business.Checkout.Interfaces.CheckoutAttributes;
using Grand.Business.Checkout.Interfaces.GiftVouchers;
using Grand.Business.Checkout.Interfaces.Orders;
using Grand.Business.Checkout.Interfaces.Payments;
using Grand.Business.Checkout.Queries.Models.Orders;
using Grand.Business.Checkout.Services.Orders;
using Grand.Business.Checkout.Utilities;
using Grand.Business.Common.Extensions;
using Grand.Business.Common.Interfaces.Directory;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Business.Common.Interfaces.Logging;
using Grand.Business.Common.Interfaces.Pdf;
using Grand.Business.Customers.Interfaces;
using Grand.Business.Messages.Interfaces;
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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

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
        private readonly IProductAttributeParser _productAttributeParser;
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
            IProductAttributeParser productAttributeParser,
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
            IPdfService pdfService,
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
            _productAttributeParser = productAttributeParser;
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

                    _ = Task.Run(() => SendNotification(_serviceScopeFactory, result.PlacedOrder, _workContext.CurrentCustomer, _workContext.OriginalCustomerIfImpersonated));

                    //check order status
                    await _mediator.Send(new CheckOrderStatusCommand() { Order = result.PlacedOrder });

                    //raise event       
                    await _mediator.Publish(new OrderPlacedEvent(result.PlacedOrder));

                    if (result.PlacedOrder.PaymentStatusId == PaymentStatus.Paid)
                    {
                        await _mediator.Send(new ProcessOrderPaidCommand() { Order = result.PlacedOrder });
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
                _logger.Error(exc.Message, exc);
                result.AddError(exc.Message);
            }

            #region Process errors

            string error = "";
            for (int i = 0; i < result.Errors.Count; i++)
            {
                error += string.Format("Error {0}: {1}", i + 1, result.Errors[i]);
                if (i != result.Errors.Count - 1)
                    error += ". ";
            }
            if (!string.IsNullOrEmpty(error))
            {
                //log it
                string logError = string.Format("Error while placing order. {0}", error);
                _logger.Error(logError);
            }

            #endregion

            return result;
        }

        protected virtual async Task<(ProcessPaymentResult paymentResult, PaymentTransaction paymentTransaction)>
           PrepareProcessPayment(PlaceOrderContainter details)
        {
            //payment transaction
            var paymentTransaction = await PreparePaymentTransaction(details);

            //process payment
            ProcessPaymentResult processPaymentResult = null;
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
                if (processPaymentResult == null)
                    processPaymentResult = new ProcessPaymentResult();
                processPaymentResult.NewPaymentTransactionStatus = TransactionStatus.Paid;
            }

            if (processPaymentResult == null)
                throw new GrandException("processPaymentResult is not available");

            return (processPaymentResult, paymentTransaction);
        }

        protected async Task<PaymentTransaction> PreparePaymentTransaction(PlaceOrderContainter details)
        {
            var update = false;
            var paymentTransaction = new PaymentTransaction() { CreatedOnUtc = DateTime.UtcNow };
            var paymentTransactionId = details.Customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.PaymentTransaction, _workContext.CurrentStore.Id);
            if (!string.IsNullOrEmpty(paymentTransactionId))
            {
                paymentTransaction = await _paymentTransactionService.GetById(paymentTransactionId);
                if (paymentTransaction != null)
                {
                    update = true;
                    paymentTransaction.UpdatedOnUtc = DateTime.UtcNow;
                }
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

        protected async Task UpdatePaymentTransaction(PaymentTransaction paymentTransaction, Order order, ProcessPaymentResult processPaymentResult)
        {
            paymentTransaction.TransactionAmount = order.OrderTotal;
            paymentTransaction.PaidAmount = order.PaidAmount;
            paymentTransaction.Temp = false;
            paymentTransaction.TransactionStatus = processPaymentResult.NewPaymentTransactionStatus;
            paymentTransaction.Errors.Clear();
            await _paymentTransactionService.UpdatePaymentTransaction(paymentTransaction);
        }
        private async Task<double?> PrepareCommissionRate(Product product, PlaceOrderContainter details)
        {
            var commissionRate = default(double?);
            if (!string.IsNullOrEmpty(product.VendorId))
            {
                var vendor = await _vendorService.GetVendorById(product.VendorId);
                if (vendor != null && vendor.Commission.HasValue)
                    commissionRate = vendor.Commission.Value;
            }

            if (!commissionRate.HasValue)
            {
                if (!string.IsNullOrEmpty(details.Customer.SeId))
                {
                    var salesEmployee = await _salesEmployeeService.GetSalesEmployeeById(details.Customer.SeId);
                    if (salesEmployee != null && salesEmployee.Active && salesEmployee.Commission.HasValue)
                        commissionRate = salesEmployee.Commission.Value;
                }
            }

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
                var attributeValues = _productAttributeParser.ParseProductAttributeValues(product, shoppingCartItem.Attributes);
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
                                if (associatedProduct != null && associatedProduct.IsShipEnabled)
                                {
                                    attributesTotalWeight += associatedProduct.Weight * attributeValue.Quantity;
                                }
                            }
                            break;
                    }
                }
            }
            var weight = product.Weight + attributesTotalWeight;
            return weight;
        }

        protected virtual async Task<PlaceOrderContainter> PreparePlaceOrderDetails()
        {
            var details = new PlaceOrderContainter
            {
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
                if (affiliate != null && affiliate.Active)
                    details.AffiliateId = affiliate.Id;
            }

            //customer currency
            var currencyTmp = await _currencyService.GetCurrencyById(details.Customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.CurrencyId, _workContext.CurrentStore.Id));
            var customerCurrency = (currencyTmp != null && currencyTmp.Published) ? currencyTmp : _workContext.WorkingCurrency;
            details.Currency = customerCurrency;
            var primaryStoreCurrency = await _currencyService.GetPrimaryStoreCurrency();
            details.CurrencyRate = customerCurrency.Rate / primaryStoreCurrency.Rate;
            details.PrimaryCurrencyCode = primaryStoreCurrency.CurrencyCode;

            //customer language
            details.Language = await _languageService.GetLanguageById(details.Customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.LanguageId, _workContext.CurrentStore.Id));

            if (details.Language == null || !details.Language.Published)
                details.Language = _workContext.WorkingLanguage;

            details.BillingAddress = details.Customer.BillingAddress;
            if (!string.IsNullOrEmpty(details.BillingAddress.CountryId))
            {
                var country = await _countryService.GetCountryById(details.BillingAddress.CountryId);
                if (country != null)
                    if (!country.AllowsBilling)
                        throw new GrandException(string.Format("Country '{0}' is not allowed for billing", country.Name));
            }

            //checkout attributes
            details.CheckoutAttributes = details.Customer.GetUserFieldFromEntity<List<CustomAttribute>>(SystemCustomerFieldNames.CheckoutAttributes, _workContext.CurrentStore.Id);
            details.CheckoutAttributeDescription = await _checkoutAttributeFormatter.FormatAttributes(details.CheckoutAttributes, details.Customer);

            //load and validate customer shopping cart
            details.Cart = details.Customer.ShoppingCartItems
                .Where(sci => sci.ShoppingCartTypeId == ShoppingCartType.ShoppingCart || sci.ShoppingCartTypeId == ShoppingCartType.Auctions)
                .LimitPerStore(_shoppingCartSettings.SharedCartBetweenStores, _workContext.CurrentStore.Id)
                .ToList();

            if (!details.Cart.Any())
                throw new GrandException("Cart is empty");

            //validate the entire shopping cart
            var warnings = await _shoppingCartValidator.GetShoppingCartWarnings(details.Cart, details.CheckoutAttributes, true);
            if (warnings.Any())
            {
                var warningsSb = new StringBuilder();
                foreach (string warning in warnings)
                {
                    warningsSb.Append(warning);
                    warningsSb.Append(";");
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
                    foreach (string warning in sciWarnings)
                    {
                        warningsSb.Append(warning);
                        warningsSb.Append(";");
                    }
                    throw new GrandException(warningsSb.ToString());
                }
            }

            //min totals validation
            bool minOrderSubtotalAmountOk = await _mediator.Send(new ValidateMinShoppingCartSubtotalAmountCommand() { Customer = _workContext.CurrentCustomer, Cart = details.Cart });
            if (!minOrderSubtotalAmountOk)
            {
                double minOrderSubtotalAmount = await _currencyService.ConvertFromPrimaryStoreCurrency(_orderSettings.MinOrderSubtotalAmount, _workContext.WorkingCurrency);
                throw new GrandException(string.Format(_translationService.GetResource("Checkout.MinOrderSubtotalAmount"), _priceFormatter.FormatPrice(minOrderSubtotalAmount, false)));
            }

            bool minmaxOrderTotalAmountOk = await _mediator.Send(new ValidateShoppingCartTotalAmountCommand() { Customer = details.Customer, Cart = details.Cart });
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
            double orderSubTotalDiscountAmount = shoppingCartSubTotal.discountAmount;
            List<ApplyDiscount> orderSubTotalAppliedDiscounts = shoppingCartSubTotal.appliedDiscounts;
            double subTotalWithoutDiscountBase = shoppingCartSubTotal.subTotalWithoutDiscount;
            double subTotalWithDiscountBase = shoppingCartSubTotal.subTotalWithDiscount;

            details.OrderSubTotalInclTax = subTotalWithoutDiscountBase;
            details.OrderSubTotalDiscountInclTax = orderSubTotalDiscountAmount;

            foreach (var disc in orderSubTotalAppliedDiscounts)
                if (!details.AppliedDiscounts.Where(x => x.DiscountId == disc.DiscountId).Any())
                    details.AppliedDiscounts.Add(disc);

            //sub total (excl tax)
            shoppingCartSubTotal = await _orderTotalCalculationService.GetShoppingCartSubTotal(details.Cart, false);
            orderSubTotalDiscountAmount = shoppingCartSubTotal.discountAmount;
            orderSubTotalAppliedDiscounts = shoppingCartSubTotal.appliedDiscounts;
            subTotalWithoutDiscountBase = shoppingCartSubTotal.subTotalWithoutDiscount;
            subTotalWithDiscountBase = shoppingCartSubTotal.subTotalWithDiscount;

            details.OrderSubTotalExclTax = subTotalWithoutDiscountBase;
            details.OrderSubTotalDiscountExclTax = orderSubTotalDiscountAmount;

            //shipping info
            bool shoppingCartRequiresShipping = shoppingCartRequiresShipping = details.Cart.RequiresShipping();

            if (shoppingCartRequiresShipping)
            {
                var pickupPoint = details.Customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.SelectedPickupPoint, _workContext.CurrentStore.Id);
                if (_shippingSettings.AllowPickUpInStore && pickupPoint != null)
                {
                    details.PickUpInStore = true;
                    details.PickupPoint = await _mediator.Send(new GetPickupPointById() { Id = pickupPoint });
                }
                else
                {
                    if (details.Customer.ShippingAddress == null)
                        throw new GrandException("Shipping address is not provided");

                    if (!CommonHelper.IsValidEmail(details.Customer.ShippingAddress.Email))
                        throw new GrandException("Email is not valid");

                    //clone shipping address
                    details.ShippingAddress = details.Customer.ShippingAddress;
                    if (!String.IsNullOrEmpty(details.ShippingAddress.CountryId))
                    {
                        var country = await _countryService.GetCountryById(details.ShippingAddress.CountryId);
                        if (country != null)
                            if (!country.AllowsShipping)
                                throw new GrandException(string.Format("Country '{0}' is not allowed for shipping", country.Name));
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

            var shoppingCartShippingTotal = await _orderTotalCalculationService.GetShoppingCartShippingTotal(details.Cart, true);
            double tax = shoppingCartShippingTotal.taxRate;
            List<ApplyDiscount> shippingTotalDiscounts = shoppingCartShippingTotal.appliedDiscounts;
            var orderShippingTotalInclTax = shoppingCartShippingTotal.shoppingCartShippingTotal;
            var orderShippingTotalExclTax = (await _orderTotalCalculationService.GetShoppingCartShippingTotal(details.Cart, false)).shoppingCartShippingTotal;
            if (!orderShippingTotalInclTax.HasValue || !orderShippingTotalExclTax.HasValue)
                throw new GrandException("Shipping total couldn't be calculated");

            foreach (var disc in shippingTotalDiscounts)
            {
                if (!details.AppliedDiscounts.Where(x => x.DiscountId == disc.DiscountId).Any())
                    details.AppliedDiscounts.Add(disc);
            }


            details.OrderShippingTotalInclTax = orderShippingTotalInclTax.Value;
            details.OrderShippingTotalExclTax = orderShippingTotalExclTax.Value;

            //payment 
            var paymentMethodSystemName = _workContext.CurrentCustomer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.SelectedPaymentMethod, _workContext.CurrentStore.Id);
            details.PaymentMethodSystemName = paymentMethodSystemName;
            double paymentAdditionalFee = await _paymentService.GetAdditionalHandlingFee(details.Cart, paymentMethodSystemName);
            details.PaymentAdditionalFeeInclTax = (await _taxService.GetPaymentMethodAdditionalFee(paymentAdditionalFee, true, details.Customer)).paymentPrice;
            details.PaymentAdditionalFeeExclTax = (await _taxService.GetPaymentMethodAdditionalFee(paymentAdditionalFee, false, details.Customer)).paymentPrice;

            //tax total
            //tax amount
            var (taxtotal, taxRates) = await _orderTotalCalculationService.GetTaxTotal(details.Cart);
            details.OrderTaxTotal = taxtotal;

            //tax rates
            foreach (var kvp in taxRates)
            {
                details.Taxes.Add(new OrderTax()
                {
                    Percent = Math.Round(kvp.Key, 2),
                    Amount = kvp.Value
                });
            }

            //order total (and applied discounts, gift vouchers, loyalty points)
            var shoppingCartTotal = await _orderTotalCalculationService.GetShoppingCartTotal(details.Cart);
            List<AppliedGiftVoucher> appliedGiftVouchers = shoppingCartTotal.appliedGiftVouchers;
            List<ApplyDiscount> orderAppliedDiscounts = shoppingCartTotal.appliedDiscounts;
            double orderDiscountAmount = shoppingCartTotal.discountAmount;
            int redeemedLoyaltyPoints = shoppingCartTotal.redeemedLoyaltyPoints;
            var orderTotal = shoppingCartTotal.shoppingCartTotal;
            if (!orderTotal.HasValue)
                throw new GrandException("Order total couldn't be calculated");

            details.OrderDiscountAmount = orderDiscountAmount;
            details.RedeemedLoyaltyPoints = redeemedLoyaltyPoints;
            details.RedeemedLoyaltyPointsAmount = shoppingCartTotal.redeemedLoyaltyPointsAmount;
            details.AppliedGiftVouchers = appliedGiftVouchers;
            details.OrderTotal = orderTotal.Value;

            //discount history
            foreach (var disc in orderAppliedDiscounts)
            {
                if (!details.AppliedDiscounts.Where(x => x.DiscountId == disc.DiscountId).Any())
                    details.AppliedDiscounts.Add(disc);
            }

            return details;
        }

        protected virtual async Task<OrderItem> PrepareOrderItem(ShoppingCartItem sc, Product product, PlaceOrderContainter details)
        {
            List<ApplyDiscount> scDiscounts;
            double discountAmount;
            double scUnitPrice = (await _pricingService.GetUnitPrice(sc, product)).unitprice;
            double scUnitPriceWithoutDisc = (await _pricingService.GetUnitPrice(sc, product, false)).unitprice;

            var subtotal = await _pricingService.GetSubTotal(sc, product, true);
            double scSubTotal = subtotal.subTotal;
            discountAmount = subtotal.discountAmount;
            scDiscounts = subtotal.appliedDiscounts;

            var prices = await _taxService.GetTaxProductPrice(product, details.Customer, scUnitPrice, scUnitPriceWithoutDisc, sc.Quantity, scSubTotal, discountAmount, _taxSettings.PricesIncludeTax);
            double scUnitPriceWithoutDiscInclTax = prices.UnitPriceWihoutDiscInclTax;
            double scUnitPriceWithoutDiscExclTax = prices.UnitPriceWihoutDiscExclTax;
            double scUnitPriceInclTax = prices.UnitPriceInclTax;
            double scUnitPriceExclTax = prices.UnitPriceExclTax;
            double scSubTotalInclTax = prices.SubTotalInclTax;
            double scSubTotalExclTax = prices.SubTotalExclTax;
            double discountAmountInclTax = prices.discountAmountInclTax;
            double discountAmountExclTax = prices.discountAmountExclTax;

            foreach (var disc in scDiscounts)
            {
                if (!details.AppliedDiscounts.Where(x => x.DiscountId == disc.DiscountId).Any())
                    details.AppliedDiscounts.Add(disc);
            }

            //attributes
            string attributeDescription = await _productAttributeFormatter.FormatAttributes(product, sc.Attributes, details.Customer);

            if (string.IsNullOrEmpty(attributeDescription) && sc.ShoppingCartTypeId == ShoppingCartType.Auctions)
                attributeDescription = _translationService.GetResource("ShoppingCart.auctionwonon") + " " + product.AvailableEndDateTimeUtc;

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
            var commision = commissionRate.HasValue ?
                Math.Round((commissionRate.Value * scSubTotal / 100), 2) : 0;

            //save order item
            var orderItem = new OrderItem
            {
                OrderItemGuid = Guid.NewGuid(),
                ProductId = sc.ProductId,
                Sku = product.FormatSku(sc.Attributes, _productAttributeParser),
                VendorId = product.VendorId,
                WarehouseId = warehouseId,
                SeId = details.Customer.SeId,
                TaxRate = Math.Round(prices.taxRate, 2),
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
                Commission = commision,
                cId = sc.cId
            };

            string reservationInfo = "";
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
            if (!string.IsNullOrEmpty(reservationInfo))
            {
                if (!string.IsNullOrEmpty(orderItem.AttributeDescription))
                {
                    orderItem.AttributeDescription += "<br>" + reservationInfo;
                }
                else
                {
                    orderItem.AttributeDescription = reservationInfo;
                }
            }
            return orderItem;
        }

        protected virtual async Task GenerateGiftVoucher(PlaceOrderContainter details, ShoppingCartItem sc, Order order, OrderItem orderItem, Product product)
        {
            _productAttributeParser.GetGiftVoucherAttribute(sc.Attributes,
                        out string giftVoucherRecipientName, out string giftVoucherRecipientEmail,
                        out string giftVoucherSenderName, out string giftVoucherSenderEmail, out string giftVoucherMessage);

            for (int i = 0; i < sc.Quantity; i++)
            {
                var amount = orderItem.UnitPriceInclTax;
                if (product.OverGiftAmount.HasValue)
                    amount = await _currencyService.ConvertFromPrimaryStoreCurrency(product.OverGiftAmount.Value, details.Currency);

                var gc = new GiftVoucher
                {
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
                    CreatedOnUtc = DateTime.UtcNow
                };
                await _giftVoucherService.InsertGiftVoucher(gc);
            }
        }

        protected virtual async Task UpdateProductReservation(Order order, PlaceOrderContainter details)
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

                if (sc.RentalStartDateUtc.HasValue && sc.RentalEndDateUtc.HasValue)
                {
                    var reservations = await _productReservationService.GetProductReservationsByProductId(product.Id, true, null);
                    var grouped = reservations.GroupBy(x => x.Resource);

                    IGrouping<string, ProductReservation> groupToBook = null;
                    foreach (var group in grouped)
                    {
                        bool groupCanBeBooked = true;
                        if (product.IncBothDate && product.IntervalUnitId == IntervalUnit.Day)
                        {
                            for (DateTime iterator = sc.RentalStartDateUtc.Value; iterator <= sc.RentalEndDateUtc.Value; iterator += new TimeSpan(24, 0, 0))
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
                            for (DateTime iterator = sc.RentalStartDateUtc.Value; iterator < sc.RentalEndDateUtc.Value; iterator += new TimeSpan(24, 0, 0))
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

                    if (groupToBook == null)
                    {
                        throw new Exception("ShoppingCart.Reservation.Nofreereservationsinthisperiod");
                    }
                    else
                    {
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
                }
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
                await _auctionService.InsertBid(new Bid()
                {
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
                await _productService.UnpublishProduct(product);
            }
        }

        protected virtual async Task UpdateBids(Order order, PlaceOrderContainter details)
        {
            foreach (var sc in details.Cart.Where(x => x.ShoppingCartTypeId == ShoppingCartType.Auctions))
            {
                var bid = (await _auctionService.GetBidsByProductId(sc.Id)).Where(x => x.CustomerId == details.Customer.Id).FirstOrDefault();
                if (bid != null)
                {
                    bid.OrderId = order.Id;
                    await _auctionService.UpdateBid(bid);
                }
            }
        }
        protected virtual async Task InsertDiscountUsageHistory(Order order, PlaceOrderContainter details)
        {
            foreach (var discount in details.AppliedDiscounts)
            {
                var duh = new DiscountUsageHistory
                {
                    DiscountId = discount.DiscountId,
                    CouponCode = discount.CouponCode,
                    OrderId = order.Id,
                    CustomerId = order.CustomerId,
                    CreatedOnUtc = DateTime.UtcNow
                };
                await _discountService.InsertDiscountUsageHistory(duh);
            }
        }

        protected virtual async Task AppliedGiftVouchers(Order order, PlaceOrderContainter details)
        {
            foreach (var agc in details.AppliedGiftVouchers)
            {
                double amountUsed = agc.AmountCanBeUsed;
                var gcuh = new GiftVoucherUsageHistory
                {
                    GiftVoucherId = agc.GiftVoucher.Id,
                    UsedWithOrderId = order.Id,
                    UsedValue = amountUsed,
                    CreatedOnUtc = DateTime.UtcNow
                };
                agc.GiftVoucher.GiftVoucherUsageHistory.Add(gcuh);
                await _giftVoucherService.UpdateGiftVoucher(agc.GiftVoucher);
            }
        }

        /// <summary>
        /// Prepare order header
        /// </summary>
        /// <returns>Order</returns>
        protected virtual Order PrepareOrderHeader(PaymentTransaction paymentTransaction,
            ProcessPaymentResult processPaymentResult, PlaceOrderContainter details)
        {
            var paymentStatus = PaymentStatus.Pending;

            if (processPaymentResult.NewPaymentTransactionStatus == TransactionStatus.Paid && processPaymentResult.PaidAmount == details.OrderTotal)
                paymentStatus = PaymentStatus.Paid;

            if (processPaymentResult.NewPaymentTransactionStatus == TransactionStatus.Paid &&
                processPaymentResult.PaidAmount > 0 &&
                processPaymentResult.PaidAmount < details.OrderTotal)
                paymentStatus = PaymentStatus.PartiallyPaid;


            var order = new Order
            {
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
        /// <param name="details">Place order containter</param>
        /// <param name="order">Order</param>
        /// <returns>Order</returns>
        protected virtual async Task<Order> SaveOrderDetails(PlaceOrderContainter details, Order order)
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
                await _productService.UpdateSold(product, sc.Quantity);
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
        /// <param name="order">Order</param>
        protected virtual async Task SendNotification(IServiceScopeFactory scopeFactory, Order order, Customer customer, Customer originalCustomerIfImpersonated)
        {
            using var scope = scopeFactory.CreateScope();

            var orderService = scope.ServiceProvider.GetRequiredService<IOrderService>();
            var messageProviderService = scope.ServiceProvider.GetRequiredService<IMessageProviderService>();
            var orderSettings = scope.ServiceProvider.GetRequiredService<OrderSettings>();
            var languageSettings = scope.ServiceProvider.GetRequiredService<LanguageSettings>();
            var pdfService = scope.ServiceProvider.GetRequiredService<IPdfService>();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger>();
            try
            {
                //notes, messages
                if (originalCustomerIfImpersonated != null)
                {
                    //this order is placed by a store administrator impersonating a customer
                    await orderService.InsertOrderNote(new OrderNote
                    {
                        Note = string.Format("Order placed by a store owner ('{0}'. ID = {1}) impersonating the customer.",
                            originalCustomerIfImpersonated.Email, originalCustomerIfImpersonated.Id),
                        DisplayToCustomer = false,
                        CreatedOnUtc = DateTime.UtcNow,
                        OrderId = order.Id,
                    });
                }
                else
                {
                    await orderService.InsertOrderNote(new OrderNote
                    {
                        Note = "Order placed",
                        DisplayToCustomer = false,
                        CreatedOnUtc = DateTime.UtcNow,
                        OrderId = order.Id,

                    });
                }

                //send email notifications
                int orderPlacedStoreOwnerNotificationQueuedEmailId = await messageProviderService.SendOrderPlacedStoreOwnerMessage(order, customer, languageSettings.DefaultAdminLanguageId);
                if (orderPlacedStoreOwnerNotificationQueuedEmailId > 0)
                {
                    await orderService.InsertOrderNote(new OrderNote
                    {
                        Note = "Order placed email (to store owner) has been queued",
                        DisplayToCustomer = false,
                        CreatedOnUtc = DateTime.UtcNow,
                        OrderId = order.Id,

                    });
                }

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
                    _logger.Error($"Error - order placed attachment file {order.OrderNumber}", ex);
                }

                int orderPlacedCustomerNotificationQueuedEmailId = await messageProviderService
                    .SendOrderPlacedCustomerMessage(order, customer, order.CustomerLanguageId, orderPlacedAttachmentFilePath, orderPlacedAttachmentFileName, orderPlacedAttachments);

                if (orderPlacedCustomerNotificationQueuedEmailId > 0)
                {
                    await orderService.InsertOrderNote(new OrderNote
                    {
                        Note = "Order placed email (to customer) has been queued",
                        DisplayToCustomer = false,
                        CreatedOnUtc = DateTime.UtcNow,
                        OrderId = order.Id,

                    });
                }
                if (order.OrderItems.Any(x => !string.IsNullOrEmpty(x.VendorId)))
                {
                    var vendors = await mediator.Send(new GetVendorsInOrderQuery() { Order = order });
                    foreach (var vendor in vendors)
                    {
                        int orderPlacedVendorNotificationQueuedEmailId = await messageProviderService.SendOrderPlacedVendorMessage(order, customer, vendor, languageSettings.DefaultAdminLanguageId);
                        if (orderPlacedVendorNotificationQueuedEmailId > 0)
                        {
                            await orderService.InsertOrderNote(new OrderNote
                            {
                                Note = "Order placed email (to vendor) has been queued",
                                DisplayToCustomer = false,
                                CreatedOnUtc = DateTime.UtcNow,
                                OrderId = order.Id,
                            });
                        }
                    }
                }
            }
            catch (Exception e)
            {
                await _logger.InsertLog(Domain.Logging.LogLevel.Error, "Place order send notifiaction error", e.Message);
            }

        }
    }
}

