using Grand.Business.Core.Commands.Checkout.Orders;
using Grand.Business.Core.Enums.Checkout;
using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Payments;
using Grand.Business.Core.Interfaces.Checkout.Shipping;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Utilities.Checkout;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Orders;
using Grand.Domain.Payments;
using Grand.Domain.Shipping;
using Grand.Infrastructure;
using Grand.Infrastructure.Extensions;
using Grand.SharedKernel.Attributes;
using Grand.Web.Common.Controllers;
using Grand.Web.Common.Filters;
using Grand.Web.Extensions;
using Grand.Web.Features.Models.Checkout;
using Grand.Web.Features.Models.Common;
using Grand.Web.Models.Checkout;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Logging;

namespace Grand.Web.Controllers;

[DenySystemAccount]
[ApiGroup(SharedKernel.Extensions.ApiConstants.ApiGroupNameV2)]
public class CheckoutController : BasePublicController
{
    #region Constructors

    public CheckoutController(
        IContextAccessor contextAccessor,
        ITranslationService translationService,
        ICustomerService customerService,
        IGroupService groupService,
        IShoppingCartService shoppingCartService,
        IShippingService shippingService,
        IPickupPointService pickupPointService,
        IPaymentService paymentService,
        IPaymentTransactionService paymentTransactionService,
        ILogger<CheckoutController> logger,
        IOrderService orderService,
        IMediator mediator,
        IProductService productService,
        IShoppingCartValidator shoppingCartValidator,
        OrderSettings orderSettings,
        LoyaltyPointsSettings loyaltyPointsSettings,
        PaymentSettings paymentSettings,
        ShippingSettings shippingSettings,
        AddressSettings addressSettings)
    {
        _contextAccessor = contextAccessor;
        _translationService = translationService;
        _customerService = customerService;
        _groupService = groupService;
        _shoppingCartService = shoppingCartService;
        _shippingService = shippingService;
        _pickupPointService = pickupPointService;
        _paymentService = paymentService;
        _paymentTransactionService = paymentTransactionService;
        _logger = logger;
        _orderService = orderService;
        _mediator = mediator;
        _productService = productService;
        _shoppingCartValidator = shoppingCartValidator;
        _orderSettings = orderSettings;
        _loyaltyPointsSettings = loyaltyPointsSettings;
        _paymentSettings = paymentSettings;
        _shippingSettings = shippingSettings;
        _addressSettings = addressSettings;
    }

    #endregion

    [HttpGet]
    public virtual async Task<IActionResult> Index()
    {
        var customer = _contextAccessor.WorkContext.CurrentCustomer;

        var cart = await _shoppingCartService.GetShoppingCart(_contextAccessor.StoreContext.CurrentStore.Id,
            ShoppingCartType.ShoppingCart, ShoppingCartType.Auctions);

        if (!cart.Any())
            return RedirectToRoute("ShoppingCart");

        if (await _groupService.IsGuest(customer) && !_orderSettings.AnonymousCheckoutAllowed)
            return Challenge();

        //reset checkout data
        await _customerService.ResetCheckoutData(customer, _contextAccessor.StoreContext.CurrentStore.Id);

        //validation (cart)
        var checkoutAttributes = customer.GetUserFieldFromEntity<List<CustomAttribute>>(SystemCustomerFieldNames.CheckoutAttributes, _contextAccessor.StoreContext.CurrentStore.Id);
        var scWarnings = await _shoppingCartValidator.GetShoppingCartWarnings(cart, checkoutAttributes, true, true);
        if (scWarnings.Any())
            return RedirectToRoute("ShoppingCart", new { checkoutAttributes = true });

        //validation (each shopping cart item)
        foreach (var sci in cart)
        {
            var product = await _productService.GetProductById(sci.ProductId);
            var sciWarnings =
                await _shoppingCartValidator.GetShoppingCartItemWarnings(customer, sci, product,
                    new ShoppingCartValidatorOptions());
            if (sciWarnings.Any())
                return RedirectToRoute("ShoppingCart", new { checkoutAttributes = true });
        }

        return RedirectToRoute("Checkout");
    }

    [HttpGet]
    public virtual async Task<IActionResult> Start()
    {
        //validation
        var cart = await _shoppingCartService.GetShoppingCart(_contextAccessor.StoreContext.CurrentStore.Id,
            ShoppingCartType.ShoppingCart, ShoppingCartType.Auctions);

        if (!cart.Any())
            return RedirectToRoute("ShoppingCart");

        if (await _groupService.IsGuest(_contextAccessor.WorkContext.CurrentCustomer) && !_orderSettings.AnonymousCheckoutAllowed)
            return Challenge();

        //validation (each shopping cart item)
        foreach (var sci in cart)
        {
            var product = await _productService.GetProductById(sci.ProductId);
            var sciWarnings = await _shoppingCartValidator.GetShoppingCartItemWarnings(_contextAccessor.WorkContext.CurrentCustomer,
                sci, product, new ShoppingCartValidatorOptions());
            if (sciWarnings.Any())
                return RedirectToRoute("ShoppingCart", new { checkoutAttributes = true });
        }

        var requiresShipping = cart.RequiresShipping();
        var model = new CheckoutModel {
            ShippingRequired = requiresShipping,
            BillingAddress = await _mediator.Send(new GetBillingAddress {
                Cart = cart,
                Currency = _contextAccessor.WorkContext.WorkingCurrency,
                Customer = _contextAccessor.WorkContext.CurrentCustomer,
                Language = _contextAccessor.WorkContext.WorkingLanguage,
                Store = _contextAccessor.StoreContext.CurrentStore,
                PrePopulateNewAddressWithCustomerFields = true
            }),
            ShippingAddress = await _mediator.Send(new GetShippingAddress {
                Currency = _contextAccessor.WorkContext.WorkingCurrency,
                Customer = _contextAccessor.WorkContext.CurrentCustomer,
                Language = _contextAccessor.WorkContext.WorkingLanguage,
                Store = _contextAccessor.StoreContext.CurrentStore,
                PrePopulateNewAddressWithCustomerFields = true
            })
        };
        if (!requiresShipping && !model.BillingAddress.ExistingAddresses.Any())
            model.BillingAddress.NewAddressPreselected = true;

        return View(model);
    }

    [HttpPost]
    public virtual async Task<IActionResult> SaveBilling(
        [FromServices] AddressSettings addressSettings,
        CheckoutBillingAddressModel model)
    {
        try
        {
            //validation
            var cart = await _shoppingCartService.GetShoppingCart(_contextAccessor.StoreContext.CurrentStore.Id,
                ShoppingCartType.ShoppingCart, ShoppingCartType.Auctions);
            await CartValidate(cart);

            if (!string.IsNullOrEmpty(model.BillingAddressId))
            {
                //existing address
                var address =
                    _contextAccessor.WorkContext.CurrentCustomer.Addresses.FirstOrDefault(a => a.Id == model.BillingAddressId);
                _contextAccessor.WorkContext.CurrentCustomer.BillingAddress =
                    address ?? throw new Exception("Address can't be loaded");
                await _customerService.UpdateBillingAddress(address, _contextAccessor.WorkContext.CurrentCustomer.Id);
            }
            else
            {
                if (!ModelState.IsValid)
                {
                    //model is not valid. redisplay the form with errors
                    var billingAddressModel = await _mediator.Send(new GetBillingAddress {
                        Cart = cart,
                        Currency = _contextAccessor.WorkContext.WorkingCurrency,
                        Customer = _contextAccessor.WorkContext.CurrentCustomer,
                        Language = _contextAccessor.WorkContext.WorkingLanguage,
                        Store = _contextAccessor.StoreContext.CurrentStore,
                        SelectedCountryId = model.BillingNewAddress.CountryId,
                        OverrideAttributes = await _mediator.Send(new GetParseCustomAddressAttributes
                            { SelectedAttributes = model.BillingNewAddress.SelectedAttributes })
                    });

                    billingAddressModel.NewAddressPreselected = true;
                    return Json(new {
                        update_section = new UpdateSectionJsonModel {
                            name = "billing",
                            model = billingAddressModel
                        },
                        wrong_billing_address = true,
                        model_state = SerializeModelState(ModelState)
                    });
                }

                //try to find an address with the same values (don't duplicate records)
                var address = _contextAccessor.WorkContext.CurrentCustomer.Addresses.ToList().FindAddress(
                    model.BillingNewAddress.FirstName, model.BillingNewAddress.LastName,
                    model.BillingNewAddress.PhoneNumber,
                    model.BillingNewAddress.Email, model.BillingNewAddress.FaxNumber,
                    model.BillingNewAddress.Company,
                    model.BillingNewAddress.Address1, model.BillingNewAddress.Address2,
                    model.BillingNewAddress.City,
                    model.BillingNewAddress.StateProvinceId, model.BillingNewAddress.ZipPostalCode,
                    model.BillingNewAddress.CountryId);
                if (address == null)
                {
                    //address is not found. create a new one
                    address =
                        await _groupService.IsGuest(_contextAccessor.WorkContext.CurrentCustomer)
                            ? model.BillingNewAddress.ToEntity()
                            : model.BillingNewAddress.ToEntity(_contextAccessor.WorkContext.CurrentCustomer, addressSettings);

                    address.Attributes = await _mediator.Send(new GetParseCustomAddressAttributes
                        { SelectedAttributes = model.BillingNewAddress.SelectedAttributes });
                    address.AddressType = _addressSettings.AddressTypeEnabled ? AddressType.Billing : AddressType.Any;

                    _contextAccessor.WorkContext.CurrentCustomer.Addresses.Add(address);
                    await _customerService.InsertAddress(address, _contextAccessor.WorkContext.CurrentCustomer.Id);
                }

                _contextAccessor.WorkContext.CurrentCustomer.BillingAddress = address;
                await _customerService.UpdateBillingAddress(address, _contextAccessor.WorkContext.CurrentCustomer.Id);
            }

            //load next step
            if (cart.RequiresShipping())
                return await LoadStepAfterBillingAddress(cart);
            //shipping is not required
            _contextAccessor.WorkContext.CurrentCustomer.ShippingAddress = null;
            await _customerService.UpdateCustomerField(_contextAccessor.WorkContext.CurrentCustomer, x => x.ShippingAddress,
                null);

            await _customerService.UpdateUserField<ShippingOption>(_contextAccessor.WorkContext.CurrentCustomer,
                SystemCustomerFieldNames.SelectedShippingOption, null, _contextAccessor.StoreContext.CurrentStore.Id);
            //load next step
            return await LoadStepAfterShippingMethod(cart);
        }
        catch (Exception exc)
        {
            _logger.LogWarning(exc.Message);
            return Json(new { error = 1, message = exc.Message });
        }
    }

    [HttpPost]
    public virtual async Task<IActionResult> SaveShipping(
        [FromServices] AddressSettings addressSettings,
        CheckoutShippingAddressModel model)
    {
        try
        {
            //validation
            var cart = await _shoppingCartService.GetShoppingCart(_contextAccessor.StoreContext.CurrentStore.Id,
                ShoppingCartType.ShoppingCart, ShoppingCartType.Auctions);
            await CartValidate(cart);

            if (!cart.RequiresShipping())
                throw new Exception("Shipping is not required");

            //Pick up in store?
            //var pickup in store = false;
            if (_shippingSettings.AllowPickUpInStore)
            {
                if (model.PickUpInStore)
                {
                    //customer decided to pick up in store
                    //no shipping address selected
                    _contextAccessor.WorkContext.CurrentCustomer.ShippingAddress = null;
                    await _customerService.UpdateCustomerField(_contextAccessor.WorkContext.CurrentCustomer, x => x.ShippingAddress,
                        null);

                    //clear shipping option XML/Description
                    await _customerService.UpdateUserField(_contextAccessor.WorkContext.CurrentCustomer,
                        SystemCustomerFieldNames.ShippingOptionAttribute, "", _contextAccessor.StoreContext.CurrentStore.Id);
                    await _customerService.UpdateUserField(_contextAccessor.WorkContext.CurrentCustomer,
                        SystemCustomerFieldNames.ShippingOptionAttributeDescription, "",
                        _contextAccessor.StoreContext.CurrentStore.Id);

                    var pickupPoints =
                        await _pickupPointService.LoadActivePickupPoints(_contextAccessor.StoreContext.CurrentStore.Id);
                    var selectedPoint = pickupPoints.FirstOrDefault(x => x.Id.Equals(model.PickupPointId));
                    if (selectedPoint == null)
                        throw new Exception("Pickup point is not allowed");

                    //save "pick up in store" shipping method
                    var pickUpInStoreShippingOption = new ShippingOption {
                        Name = string.Format(_translationService.GetResource("Checkout.PickupPoints.Name"),
                            selectedPoint.Name),
                        Rate = selectedPoint.PickupFee,
                        Description = selectedPoint.Description,
                        ShippingRateProviderSystemName = $"PickupPoint_{selectedPoint.Id}"
                    };

                    await _customerService.UpdateUserField(_contextAccessor.WorkContext.CurrentCustomer,
                        SystemCustomerFieldNames.SelectedShippingOption,
                        pickUpInStoreShippingOption,
                        _contextAccessor.StoreContext.CurrentStore.Id);

                    await _customerService.UpdateUserField(_contextAccessor.WorkContext.CurrentCustomer,
                        SystemCustomerFieldNames.SelectedPickupPoint,
                        selectedPoint.Id,
                        _contextAccessor.StoreContext.CurrentStore.Id);
                }
                else
                    //set value indicating that "pick up in store" option has not been chosen
                {
                    await _customerService.UpdateUserField(_contextAccessor.WorkContext.CurrentCustomer,
                        SystemCustomerFieldNames.SelectedPickupPoint, "", _contextAccessor.StoreContext.CurrentStore.Id);
                }
            }

            if (!model.PickUpInStore)
            {
                if (!string.IsNullOrEmpty(model.ShippingAddressId))
                {
                    //existing address
                    var address =
                        _contextAccessor.WorkContext.CurrentCustomer.Addresses.FirstOrDefault(a => a.Id == model.ShippingAddressId);
                    if (address == null)
                        throw new Exception("Address can't be loaded");

                    _contextAccessor.WorkContext.CurrentCustomer.ShippingAddress = address;
                    await _customerService.UpdateShippingAddress(address, _contextAccessor.WorkContext.CurrentCustomer.Id);
                }
                else
                {
                    if (!ModelState.IsValid)
                    {
                        //model is not valid. redisplay the form with errors
                        var shippingAddressModel = await _mediator.Send(new GetShippingAddress {
                            Currency = _contextAccessor.WorkContext.WorkingCurrency,
                            Customer = _contextAccessor.WorkContext.CurrentCustomer,
                            Language = _contextAccessor.WorkContext.WorkingLanguage,
                            Store = _contextAccessor.StoreContext.CurrentStore,
                            SelectedCountryId = model.ShippingNewAddress.CountryId,
                            OverrideAttributes = await _mediator.Send(new GetParseCustomAddressAttributes
                                { SelectedAttributes = model.ShippingNewAddress.SelectedAttributes })
                        });

                        shippingAddressModel.NewAddressPreselected = true;
                        return Json(new {
                            update_section = new UpdateSectionJsonModel {
                                name = "shipping",
                                model = shippingAddressModel
                            },
                            wrong_shipping_address = true,
                            model_state = SerializeModelState(ModelState)
                        });
                    }

                    //try to find an address with the same values (don't duplicate records)
                    var address = _contextAccessor.WorkContext.CurrentCustomer.Addresses.ToList().FindAddress(
                        model.ShippingNewAddress.FirstName, model.ShippingNewAddress.LastName,
                        model.ShippingNewAddress.PhoneNumber,
                        model.ShippingNewAddress.Email, model.ShippingNewAddress.FaxNumber,
                        model.ShippingNewAddress.Company,
                        model.ShippingNewAddress.Address1, model.ShippingNewAddress.Address2,
                        model.ShippingNewAddress.City,
                        model.ShippingNewAddress.StateProvinceId, model.ShippingNewAddress.ZipPostalCode,
                        model.ShippingNewAddress.CountryId);
                    if (address == null)
                    {
                        address =
                            await _groupService.IsGuest(_contextAccessor.WorkContext.CurrentCustomer)
                                ? model.ShippingNewAddress.ToEntity()
                                : model.ShippingNewAddress.ToEntity(_contextAccessor.WorkContext.CurrentCustomer, addressSettings);

                        address.Attributes = await _mediator.Send(new GetParseCustomAddressAttributes
                            { SelectedAttributes = model.ShippingNewAddress.SelectedAttributes });
                        address.AddressType = _addressSettings.AddressTypeEnabled
                            ? model.BillToTheSameAddress ? AddressType.Any : AddressType.Shipping
                            : AddressType.Any;
                        //other null validations
                        _contextAccessor.WorkContext.CurrentCustomer.Addresses.Add(address);
                        await _customerService.InsertAddress(address, _contextAccessor.WorkContext.CurrentCustomer.Id);
                    }

                    _contextAccessor.WorkContext.CurrentCustomer.ShippingAddress = address;
                    await _customerService.UpdateShippingAddress(address, _contextAccessor.WorkContext.CurrentCustomer.Id);
                }
            }

            if (model.BillToTheSameAddress && !model.PickUpInStore &&
                _contextAccessor.WorkContext.CurrentCustomer.ShippingAddress!.AddressType != AddressType.Shipping)
            {
                _contextAccessor.WorkContext.CurrentCustomer.BillingAddress = _contextAccessor.WorkContext.CurrentCustomer.ShippingAddress;
                await _customerService.UpdateBillingAddress(_contextAccessor.WorkContext.CurrentCustomer.BillingAddress,
                    _contextAccessor.WorkContext.CurrentCustomer.Id);
                await _customerService.UpdateUserField<ShippingOption>(_contextAccessor.WorkContext.CurrentCustomer,
                    SystemCustomerFieldNames.SelectedShippingOption, null, _contextAccessor.StoreContext.CurrentStore.Id);
                return await LoadStepAfterBillingAddress(cart);
            }

            var billingAddressModel = await _mediator.Send(new GetBillingAddress {
                Cart = cart,
                Currency = _contextAccessor.WorkContext.WorkingCurrency,
                Customer = _contextAccessor.WorkContext.CurrentCustomer,
                Language = _contextAccessor.WorkContext.WorkingLanguage,
                Store = _contextAccessor.StoreContext.CurrentStore,
                SelectedCountryId = model.ShippingNewAddress.CountryId
            });
            if (!billingAddressModel.ExistingAddresses.Any())
                billingAddressModel.NewAddressPreselected = true;

            return Json(new {
                update_section = new UpdateSectionJsonModel {
                    name = "billing",
                    model = billingAddressModel
                },
                goto_section = "billing"
            });
        }
        catch (Exception exc)
        {
            _logger.LogWarning(exc.Message);
            return Json(new { error = 1, message = exc.Message });
        }
    }

    [HttpPost]
    public virtual async Task<IActionResult> SaveShippingMethod(CheckoutShippingMethodModel model)
    {
        try
        {
            //validation
            var customer = _contextAccessor.WorkContext.CurrentCustomer;
            var store = _contextAccessor.StoreContext.CurrentStore;

            var cart = await _shoppingCartService.GetShoppingCart(_contextAccessor.StoreContext.CurrentStore.Id,
                ShoppingCartType.ShoppingCart, ShoppingCartType.Auctions);
            await CartValidate(cart);

            if (!cart.RequiresShipping())
                throw new Exception("Shipping is not required");

            //parse selected method 
            //model.TryGetValue("shipping option", out var shipping);
            if (string.IsNullOrEmpty(model.ShippingOption))
                throw new Exception("Selected shipping method can't be parsed");

            var splitOption = model.ShippingOption.Split([":"], StringSplitOptions.RemoveEmptyEntries);
            if (splitOption.Length != 2)
                throw new Exception("Selected shipping method can't be parsed");

            var selectedName = splitOption[0];
            var shippingRateProviderSystemName = splitOption[1];

            //clear shipping option XML/Description
            await _customerService.UpdateUserField(customer, SystemCustomerFieldNames.ShippingOptionAttribute, "",
                store.Id);
            await _customerService.UpdateUserField(customer, SystemCustomerFieldNames.ShippingOptionAttributeDescription,
                "", store.Id);

            //validate customer's input
            var warnings = (await ValidateShippingForm(model)).ToList();

            //find it
            //performance optimization. try cache first
            var shippingOptions = customer.GetUserFieldFromEntity<List<ShippingOption>>(SystemCustomerFieldNames.OfferedShippingOptions, store.Id);
            if (shippingOptions == null || shippingOptions.Count == 0)
                //not found? load them using shipping service
                shippingOptions = (await _shippingService
                        .GetShippingOptions(customer, cart, customer.ShippingAddress,
                            shippingRateProviderSystemName, store))
                    .ShippingOptions
                    .ToList();
            else
                //loaded cached results. filter result by a chosen Shipping rate  method
                shippingOptions = shippingOptions.Where(so =>
                        so.ShippingRateProviderSystemName.Equals(shippingRateProviderSystemName,
                            StringComparison.OrdinalIgnoreCase))
                    .ToList();

            var shippingOption = shippingOptions
                .Find(so => !string.IsNullOrEmpty(so.Name) &&
                            so.Name.Equals(selectedName, StringComparison.OrdinalIgnoreCase));
            if (shippingOption == null)
                throw new Exception("Selected shipping method can't be loaded");

            //save
            await _customerService.UpdateUserField(customer, SystemCustomerFieldNames.SelectedShippingOption,
                shippingOption, store.Id);

            if (!warnings.Any())
                //load next step
                return await LoadStepAfterShippingMethod(cart);

            var message = string.Join(", ", warnings.ToArray());
            return Json(new { error = 1, message });
        }
        catch (Exception exc)
        {
            _logger.LogWarning(exc.Message);
            return Json(new { error = 1, message = exc.Message });
        }
    }

    [HttpGet]
    public virtual async Task<IActionResult> GetShippingFormPartialView(string shippingOption)
    {
        var routeName = await GetShippingComputation(shippingOption).GetControllerRouteName();
        if (string.IsNullOrEmpty(routeName))
            return Content("");

        return RedirectToRoute(routeName, new { shippingOption });
    }

    [HttpPost]
    public virtual async Task<IActionResult> SavePaymentMethod(CheckoutPaymentMethodModel model)
    {
        try
        {
            //validation
            var cart = await _shoppingCartService.GetShoppingCart(_contextAccessor.StoreContext.CurrentStore.Id,
                ShoppingCartType.ShoppingCart, ShoppingCartType.Auctions);
            await CartValidate(cart);

            //payment method 
            if (string.IsNullOrEmpty(model.PaymentMethod))
                throw new Exception("Selected payment method can't be parsed");

            //loyalty points
            if (_loyaltyPointsSettings.Enabled)
                await _customerService.UpdateUserField(_contextAccessor.WorkContext.CurrentCustomer,
                    SystemCustomerFieldNames.UseLoyaltyPointsDuringCheckout, model.UseLoyaltyPoints,
                    _contextAccessor.StoreContext.CurrentStore.Id);

            //Check whether payment workflow is required
            var isPaymentWorkflowRequired =
                await _mediator.Send(new GetIsPaymentWorkflowRequired { Cart = cart });
            if (!isPaymentWorkflowRequired)
            {
                //payment is not required
                await _customerService.UpdateUserField<string>(_contextAccessor.WorkContext.CurrentCustomer,
                    SystemCustomerFieldNames.SelectedPaymentMethod, null, _contextAccessor.StoreContext.CurrentStore.Id);

                var confirmOrderModel = await _mediator.Send(new GetConfirmOrder {
                    Cart = cart, Customer = _contextAccessor.WorkContext.CurrentCustomer, Language = _contextAccessor.WorkContext.WorkingLanguage,
                    Store = _contextAccessor.StoreContext.CurrentStore
                });
                return Json(new {
                    update_section = new UpdateSectionJsonModel {
                        name = "confirm-order",
                        model = confirmOrderModel
                    },
                    goto_section = "confirm_order"
                });
            }

            var paymentMethodInst = _paymentService.LoadPaymentMethodBySystemName(model.PaymentMethod);
            if (paymentMethodInst == null ||
                !paymentMethodInst.IsPaymentMethodActive(_paymentSettings) ||
                !paymentMethodInst.IsAuthenticateStore(_contextAccessor.StoreContext.CurrentStore))
                throw new Exception("Selected payment method can't be parsed");

            //save
            await _customerService.UpdateUserField(_contextAccessor.WorkContext.CurrentCustomer,
                SystemCustomerFieldNames.SelectedPaymentMethod, model.PaymentMethod, _contextAccessor.StoreContext.CurrentStore.Id);

            var paymentTransaction = await paymentMethodInst.InitPaymentTransaction();
            if (paymentTransaction != null)
                await _customerService.UpdateUserField(_contextAccessor.WorkContext.CurrentCustomer,
                    SystemCustomerFieldNames.PaymentTransaction, paymentTransaction.Id,
                    _contextAccessor.StoreContext.CurrentStore.Id);
            else
                await _customerService.UpdateUserField<string>(_contextAccessor.WorkContext.CurrentCustomer,
                    SystemCustomerFieldNames.PaymentTransaction, null, _contextAccessor.StoreContext.CurrentStore.Id);

            return await LoadStepAfterPaymentMethod(paymentMethodInst, cart);
        }
        catch (Exception exc)
        {
            _logger.LogWarning(exc.Message);
            return Json(new { error = 1, message = exc.Message });
        }
    }

    [HttpPost]
    public virtual async Task<IActionResult> SavePaymentInfo(IDictionary<string, string> model)
    {
        try
        {
            //validation
            var cart = await _shoppingCartService.GetShoppingCart(_contextAccessor.StoreContext.CurrentStore.Id,
                ShoppingCartType.ShoppingCart, ShoppingCartType.Auctions);
            await CartValidate(cart);

            var paymentMethodSystemName = _contextAccessor.WorkContext.CurrentCustomer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.SelectedPaymentMethod, _contextAccessor.StoreContext.CurrentStore.Id);
            var paymentMethod = _paymentService.LoadPaymentMethodBySystemName(paymentMethodSystemName);
            if (paymentMethod == null)
                throw new Exception("Payment method is not selected");

            var warnings = await paymentMethod.ValidatePaymentForm(model);
            if (!warnings.Any())
            {
                //save payment info
                var paymentTransaction = await paymentMethod.SavePaymentInfo(model);
                if (paymentTransaction != null)
                    //save
                    await _customerService.UpdateUserField(_contextAccessor.WorkContext.CurrentCustomer,
                        SystemCustomerFieldNames.PaymentTransaction, paymentTransaction.Id,
                        _contextAccessor.StoreContext.CurrentStore.Id);

                var confirmOrderModel = await _mediator.Send(new GetConfirmOrder {
                    Cart = cart, Customer = _contextAccessor.WorkContext.CurrentCustomer, Language = _contextAccessor.WorkContext.WorkingLanguage,
                    Store = _contextAccessor.StoreContext.CurrentStore
                });
                return Json(new {
                    update_section = new UpdateSectionJsonModel {
                        name = "confirm-order",
                        model = confirmOrderModel
                    },
                    goto_section = "confirm_order"
                });
            }

            //If we got this far, something failed, redisplay form
            var paymentInfoModel = await _mediator.Send(new GetPaymentInfo { PaymentMethod = paymentMethod });
            return Json(new {
                update_section = new UpdateSectionJsonModel {
                    name = "payment-info",
                    model = paymentInfoModel
                },
                warnings = warnings.ToArray()
            });
        }
        catch (Exception exc)
        {
            _logger.LogWarning(exc.Message);
            return Json(new { error = 1, message = exc.Message });
        }
    }

    [HttpGet]
    public virtual async Task<IActionResult> Completed(string orderId)
    {
        //validation
        if (await _groupService.IsGuest(_contextAccessor.WorkContext.CurrentCustomer) && !_orderSettings.AnonymousCheckoutAllowed)
            return Challenge();

        Order order = null;
        if (!string.IsNullOrEmpty(orderId)) order = await _orderService.GetOrderById(orderId);

        order ??= (await _orderService.SearchOrders(_contextAccessor.StoreContext.CurrentStore.Id,
                customerId: _contextAccessor.WorkContext.CurrentCustomer.Id, pageSize: 1))
            .FirstOrDefault();

        if (order == null || order.Deleted || _contextAccessor.WorkContext.CurrentCustomer.Id != order.CustomerId)
            return RedirectToRoute("HomePage");

        //disable "order completed" page?
        if (_orderSettings.DisableOrderCompletedPage)
            return RedirectToRoute("OrderDetails", new { orderId = order.Id });

        //model
        var model = new CheckoutCompletedModel {
            OrderId = order.Id,
            OrderNumber = order.OrderNumber,
            OrderCode = order.Code
        };

        return View(model);
    }

    [HttpPost]
    public virtual async Task<IActionResult> ConfirmOrder()
    {
        try
        {
            //validation
            var cart = await _shoppingCartService.GetShoppingCart(_contextAccessor.StoreContext.CurrentStore.Id,
                ShoppingCartType.ShoppingCart, ShoppingCartType.Auctions);
            await CartValidate(cart);

            //prevent 2 orders being placed within an X seconds time frame
            if (!await _mediator.Send(new GetMinOrderPlaceIntervalValid {
                    Customer = _contextAccessor.WorkContext.CurrentCustomer,
                    Store = _contextAccessor.StoreContext.CurrentStore
                }))
                return Json(new {
                    error = 1, message = _translationService.GetResource("Checkout.MinOrderPlacementInterval")
                });

            var placeOrderResult = await _mediator.Send(new PlaceOrderCommand());
            if (placeOrderResult.Success)
            {
                var paymentMethod =
                    _paymentService.LoadPaymentMethodBySystemName(placeOrderResult.PaymentTransaction
                        .PaymentMethodSystemName);
                if (paymentMethod == null)
                    //payment method could be null if order total is 0
                    //success
                    return Json(new { success = 1 });

                if (paymentMethod.PaymentMethodType == PaymentMethodType.Redirection)
                {
                    //Redirection will not work because it's AJAX request.
                    var storeLocation = _contextAccessor.StoreContext.CurrentHost.Url.TrimEnd('/');
                    //redirect
                    return Json(new {
                        redirect =
                            $"{storeLocation}/checkout/CompleteRedirectionPayment?paymentTransactionId={placeOrderResult.PaymentTransaction.Id}"
                    });
                }

                await _paymentService.PostProcessPayment(placeOrderResult.PaymentTransaction);
                //success
                return Json(new { success = 1 });
            }

            //error
            var confirmOrderModel = await _mediator.Send(new GetConfirmOrder {
                Cart = cart, Customer = _contextAccessor.WorkContext.CurrentCustomer, Language = _contextAccessor.WorkContext.WorkingLanguage,
                Store = _contextAccessor.StoreContext.CurrentStore
            });
            foreach (var error in placeOrderResult.Errors)
                confirmOrderModel.Warnings.Add(error);

            return Json(new {
                update_section = new UpdateSectionJsonModel {
                    name = "confirm-order",
                    model = confirmOrderModel
                },
                goto_section = "confirm_order"
            });
        }
        catch (Exception exc)
        {
            _logger.LogWarning(exc.Message);
            return Json(new { error = 1, message = exc.Message });
        }
    }

    [HttpGet]
    public virtual async Task<IActionResult> CompleteRedirectionPayment(string paymentTransactionId)
    {
        try
        {
            if (await _groupService.IsGuest(_contextAccessor.WorkContext.CurrentCustomer) &&
                !_orderSettings.AnonymousCheckoutAllowed)
                return Challenge();

            var paymentTransaction = await _paymentTransactionService.GetById(paymentTransactionId);
            if (paymentTransaction == null)
                return RedirectToRoute("HomePage");

            //get the order
            var order = await _orderService.GetOrderByGuid(paymentTransaction.OrderGuid);
            if (order == null)
                return RedirectToRoute("HomePage");

            if (paymentTransaction.OrderGuid != order.OrderGuid)
                return RedirectToRoute("HomePage");

            var paymentMethod =
                _paymentService.LoadPaymentMethodBySystemName(paymentTransaction.PaymentMethodSystemName);
            if (paymentMethod == null)
                return RedirectToRoute("HomePage");
            if (paymentMethod.PaymentMethodType != PaymentMethodType.Redirection)
                return RedirectToRoute("HomePage");

            if ((DateTime.UtcNow - order.CreatedOnUtc).TotalMinutes > 5)
                return RedirectToRoute("HomePage");

            // Get the redirect URL from PostRedirectPayment
            var redirectUrl = await _paymentService.PostRedirectPayment(paymentTransaction);
            if (!string.IsNullOrEmpty(redirectUrl))
                return Redirect(redirectUrl);
            
            return RedirectToRoute("CheckoutCompleted", new { orderId = order.Id });
        }
        catch (Exception exc)
        {
            _logger.LogWarning(exc.Message);
            return Content(exc.Message);
        }
    }

    #region Fields

    private readonly IContextAccessor _contextAccessor;
    private readonly ITranslationService _translationService;
    private readonly ICustomerService _customerService;
    private readonly IGroupService _groupService;
    private readonly IShoppingCartService _shoppingCartService;
    private readonly IShippingService _shippingService;
    private readonly IPickupPointService _pickupPointService;
    private readonly IPaymentService _paymentService;
    private readonly IPaymentTransactionService _paymentTransactionService;
    private readonly ILogger<CheckoutController> _logger;
    private readonly IOrderService _orderService;
    private readonly IMediator _mediator;
    private readonly IProductService _productService;
    private readonly IShoppingCartValidator _shoppingCartValidator;
    private readonly OrderSettings _orderSettings;
    private readonly LoyaltyPointsSettings _loyaltyPointsSettings;
    private readonly PaymentSettings _paymentSettings;
    private readonly ShippingSettings _shippingSettings;
    private readonly AddressSettings _addressSettings;

    #endregion

    #region Utilities

    [NonAction]
    protected IShippingRateCalculationProvider GetShippingComputation(string input)
    {
        var shippingMethodName = input.Split([":"], StringSplitOptions.RemoveEmptyEntries)[1];
        var shippingMethod = _shippingService.LoadShippingRateCalculationProviderBySystemName(shippingMethodName);
        if (shippingMethod == null)
            throw new Exception("Shipping method is not selected");

        return shippingMethod;
    }

    [NonAction]
    private async Task<IList<string>> ValidateShippingForm(CheckoutShippingMethodModel model)
    {
        var warnings = (await GetShippingComputation(model.ShippingOption)
            .ValidateShippingForm(model.ShippingOption, model.Data)).ToList();
        return warnings;
    }

    private IList<string> SerializeModelState(ModelStateDictionary modelState)
    {
        var errors = new List<string>();
        var valuerrors = modelState.Where(entry => entry.Value.Errors.Any());
        foreach (var item in valuerrors) errors.AddRange(item.Value.Errors.Select(er => er.ErrorMessage));

        return errors;
    }
    #endregion

    #region Private methods

    private async Task CartValidate(IList<ShoppingCartItem> cart)
    {
        if (!cart.Any())
            throw new Exception("Your cart is empty");

        if (await _groupService.IsGuest(_contextAccessor.WorkContext.CurrentCustomer) && !_orderSettings.AnonymousCheckoutAllowed)
            throw new Exception("Anonymous checkout is not allowed");
    }

    [NonAction]
    private async Task<JsonResult> LoadStepAfterBillingAddress(IList<ShoppingCartItem> cart)
    {
        var shippingMethodModel = await _mediator.Send(new GetShippingMethod {
            Cart = cart,
            Currency = _contextAccessor.WorkContext.WorkingCurrency,
            Customer = _contextAccessor.WorkContext.CurrentCustomer,
            Language = _contextAccessor.WorkContext.WorkingLanguage,
            ShippingAddress = _contextAccessor.WorkContext.CurrentCustomer.ShippingAddress,
            Store = _contextAccessor.StoreContext.CurrentStore
        });

        var selectedPickupPoint =
            _contextAccessor.WorkContext.CurrentCustomer.GetUserFieldFromEntity<string>(
                SystemCustomerFieldNames.SelectedPickupPoint, _contextAccessor.StoreContext.CurrentStore.Id);

        if ((!_shippingSettings.SkipShippingMethodSelectionIfOnlyOne ||
             shippingMethodModel.ShippingMethods.Count != 1) &&
            (!_shippingSettings.AllowPickUpInStore || string.IsNullOrEmpty(selectedPickupPoint)))
            return Json(new {
                update_section = new UpdateSectionJsonModel {
                    name = "shipping-method",
                    model = shippingMethodModel
                },
                goto_section = "shipping_method"
            });
        if (!(_shippingSettings.AllowPickUpInStore && !string.IsNullOrEmpty(selectedPickupPoint)))
            await _customerService.UpdateUserField(_contextAccessor.WorkContext.CurrentCustomer,
                SystemCustomerFieldNames.SelectedShippingOption,
                shippingMethodModel.ShippingMethods.First().ShippingOption,
                _contextAccessor.StoreContext.CurrentStore.Id);

        //load next step
        return await LoadStepAfterShippingMethod(cart);
    }

    [NonAction]
    private async Task<JsonResult> LoadStepAfterShippingMethod(IList<ShoppingCartItem> cart)
    {
        //Check whether payment workflow is required
        //we ignore loyalty points during cart total calculation
        var isPaymentWorkflowRequired = await _mediator.Send(new GetIsPaymentWorkflowRequired
            { Cart = cart, UseLoyaltyPoints = false });
        if (isPaymentWorkflowRequired)
        {
            //filter by country
            var filterByCountryId = "";
            if (_addressSettings.CountryEnabled &&
                _contextAccessor.WorkContext.CurrentCustomer.BillingAddress != null &&
                !string.IsNullOrEmpty(_contextAccessor.WorkContext.CurrentCustomer.BillingAddress.CountryId))
                filterByCountryId = _contextAccessor.WorkContext.CurrentCustomer.BillingAddress.CountryId;

            //payment is required
            var paymentMethodModel = await _mediator.Send(new GetPaymentMethod {
                Cart = cart,
                Currency = _contextAccessor.WorkContext.WorkingCurrency,
                Customer = _contextAccessor.WorkContext.CurrentCustomer,
                FilterByCountryId = filterByCountryId,
                Language = _contextAccessor.WorkContext.WorkingLanguage,
                Store = _contextAccessor.StoreContext.CurrentStore
            });

            if (!_paymentSettings.SkipPaymentIfOnlyOne ||
                paymentMethodModel.PaymentMethods.Count != 1 || paymentMethodModel.DisplayLoyaltyPoints)
                return Json(new {
                    update_section = new UpdateSectionJsonModel {
                        name = "payment-method",
                        model = paymentMethodModel
                    },
                    goto_section = "payment_method"
                });
            //if we have only one payment method and loyalty points are disabled or the current customer doesn't have any loyalty points
            //so customer doesn't have to choose a payment method

            var selectedPaymentMethodSystemName = paymentMethodModel.PaymentMethods[0].PaymentMethodSystemName;
            await _customerService.UpdateUserField(_contextAccessor.WorkContext.CurrentCustomer,
                SystemCustomerFieldNames.SelectedPaymentMethod,
                selectedPaymentMethodSystemName, _contextAccessor.StoreContext.CurrentStore.Id);

            var paymentMethodInst =
                _paymentService.LoadPaymentMethodBySystemName(selectedPaymentMethodSystemName);
            if (paymentMethodInst == null ||
                !paymentMethodInst.IsPaymentMethodActive(_paymentSettings) ||
                !paymentMethodInst.IsAuthenticateStore(_contextAccessor.StoreContext.CurrentStore))
                throw new Exception("Selected payment method can't be parsed");

            return await LoadStepAfterPaymentMethod(paymentMethodInst, cart);

            //customer have to choose a payment method
        }

        //payment is not required
        await _customerService.UpdateUserField<string>(_contextAccessor.WorkContext.CurrentCustomer,
            SystemCustomerFieldNames.SelectedPaymentMethod, null, _contextAccessor.StoreContext.CurrentStore.Id);

        var confirmOrderModel = await _mediator.Send(new GetConfirmOrder {
            Cart = cart, Customer = _contextAccessor.WorkContext.CurrentCustomer, Language = _contextAccessor.WorkContext.WorkingLanguage,
            Store = _contextAccessor.StoreContext.CurrentStore
        });
        return Json(new {
            update_section = new UpdateSectionJsonModel {
                name = "confirm-order",
                model = confirmOrderModel
            },
            goto_section = "confirm_order"
        });
    }

    [NonAction]
    private async Task<JsonResult> LoadStepAfterPaymentMethod(IPaymentProvider paymentMethod,
        IList<ShoppingCartItem> cart)
    {
        if (await paymentMethod.SkipPaymentInfo() ||
            (paymentMethod.PaymentMethodType == PaymentMethodType.Redirection
             && _paymentSettings.SkipPaymentInfo))
        {
            var confirmOrderModel = await _mediator.Send(new GetConfirmOrder {
                Cart = cart, Customer = _contextAccessor.WorkContext.CurrentCustomer, Language = _contextAccessor.WorkContext.WorkingLanguage,
                Store = _contextAccessor.StoreContext.CurrentStore
            });
            return Json(new {
                update_section = new UpdateSectionJsonModel {
                    name = "confirm-order",
                    model = confirmOrderModel
                },
                goto_section = "confirm_order"
            });
        }

        //return payment info page
        var paymenInfoModel = await _mediator.Send(new GetPaymentInfo { PaymentMethod = paymentMethod });
        return Json(new {
            update_section = new UpdateSectionJsonModel {
                name = "payment-info",
                model = paymenInfoModel
            },
            goto_section = "payment_info"
        });
    }

    #endregion
}