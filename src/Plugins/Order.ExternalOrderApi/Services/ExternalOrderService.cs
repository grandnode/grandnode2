using Grand.Business.Core.Commands.Checkout.Orders;
using Grand.Business.Core.Interfaces.Authentication;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Orders;
using Grand.Infrastructure;
using MediatR;
using Microsoft.Extensions.Logging;
using Order.ExternalOrderApi.Models;
using OrderEntity = Grand.Domain.Orders.Order;

namespace Order.ExternalOrderApi.Services;

public class ExternalOrderService : IExternalOrderService
{

    private readonly ILogger<ExternalOrderService> _logger;
    private readonly ICustomerService _customerService;
    private readonly IGroupService _groupService;
    private readonly IProductService _productService;
    private readonly ICountryService _countryService;
    private readonly IWorkContextSetter _workContextSetter;
    private readonly IContextAccessor _contextAccessor;
    private readonly IGrandAuthenticationService _authenticationService;
    private readonly IMediator _mediator;


    public ExternalOrderService(
        ILogger<ExternalOrderService> logger,
        ICustomerService customerService,
        IGroupService groupService,
        IProductService productService,
        ICountryService countryService,
        IWorkContextSetter workContextSetter,
        IContextAccessor contextAccessor,
        IGrandAuthenticationService authenticationService,
        IMediator mediator)
    {
        _logger = logger;
        _customerService = customerService;
        _groupService = groupService;
        _productService = productService;
        _countryService = countryService;
        _workContextSetter = workContextSetter;
        _contextAccessor = contextAccessor;
        _mediator = mediator;
        _authenticationService = authenticationService;
    }

    public async Task<OrderProcessResult> ProcessOrder(ExternalOrderModel externalOrderModel)
    {
        var result = new OrderProcessResult();

        if (externalOrderModel.Lines == null || externalOrderModel.Lines.Count == 0)
        {
            _logger.LogError("External order API validation error: Lines collection cannot be empty");
            result.Errors.Add("Lines collection cannot be empty");
            result.IsSuccess = false;
            return result;
        }

        var allSkus = externalOrderModel.Lines.Select(l => l.Sku).ToList();
        var skuValidation = await ValidateProducts(allSkus);

        if (!skuValidation.IsValid)
        {
            _logger.LogError("Invalid SKUs found: {InvalidSkus}", string.Join(", ", skuValidation.MissingSkus));

            result.Errors = skuValidation.MissingSkus.Select(sku => $"Product with SKU '{sku}' was not found").ToList();
            result.IsSuccess = false;
            return result;
        }

        var customer = await GetOrCreateCustomer(externalOrderModel);


        await CreateShoppingCartItems(customer, externalOrderModel, skuValidation.Products);

        string paymentMethodSystemName = "Payments.CashOnDelivery";
        await SetPaymentMethod(customer, paymentMethodSystemName);

        var placeOrderResult = await PlaceOrder(customer);

        if (!placeOrderResult.IsSuccess)
        {
            _logger.LogError("Failed to place order: {Errors}", string.Join(", ", placeOrderResult.Errors));
            return placeOrderResult;
        }

        _logger.LogInformation("Successfully placed order with ID {OrderId}, number {OrderNumber}",
            placeOrderResult.Order?.Id, placeOrderResult.Order?.OrderNumber);

        return placeOrderResult;
    }

    public async Task<(bool IsValid, IList<Product> Products, IList<string> MissingSkus)> ValidateProducts(IList<string> skus)
    {
        var result = new List<Product>();
        var missingSkus = new List<string>();

        _logger.LogInformation("Validating {Count} product SKUs", skus.Count);

        foreach (var sku in skus)
        {
            if (string.IsNullOrEmpty(sku))
            {
                _logger.LogWarning("Empty SKU found in order");
                missingSkus.Add("(empty SKU)");
                continue;
            }

            var product = await _productService.GetProductBySku(sku);
            if (product == null)
            {
                _logger.LogWarning("Product with SKU {Sku} not found", sku);
                missingSkus.Add(sku);
                continue;
            }

            if (!product.Published)
            {
                _logger.LogWarning("Product with SKU {Sku} is not published", sku);
                missingSkus.Add(sku);
                continue;
            }

            _logger.LogDebug("Found valid product with SKU {Sku}, ID {Id}", sku, product.Id);
            result.Add(product);
        }

        return (missingSkus.Count == 0, result, missingSkus);
    }

    public async Task<Customer> GetOrCreateCustomer(ExternalOrderModel model)
    {
        _logger.LogInformation("Getting or creating customer with email {Email}", model.CustomerEmail);

        var customer = await _customerService.GetCustomerByEmail(model.CustomerEmail);

        if (customer == null)
        {
            _logger.LogInformation("Customer not found. Creating new guest customer with email {Email}", model.CustomerEmail);

            customer = new Customer {
                Email = model.CustomerEmail,
                Username = model.CustomerEmail,
                Active = true,
                CreatedOnUtc = DateTime.UtcNow,
                LastActivityDateUtc = DateTime.UtcNow,
            };

            if (!string.IsNullOrEmpty(model.CustomerFirstName))
                await _customerService.UpdateUserField(customer, SystemCustomerFieldNames.FirstName, model.CustomerFirstName);

            if (!string.IsNullOrEmpty(model.CustomerLastName))
                await _customerService.UpdateUserField(customer, SystemCustomerFieldNames.LastName, model.CustomerLastName);

            var guestGroup = await _groupService.GetCustomerGroupBySystemName(SystemCustomerGroupNames.Guests);
            if (guestGroup != null)
                customer.Groups.Add(guestGroup.Id);

            var registeredGroup = await _groupService.GetCustomerGroupBySystemName(SystemCustomerGroupNames.Registered);
            if (registeredGroup != null)
            {
                customer.Groups.Add(registeredGroup.Id);
                _logger.LogDebug("Added customer to Registered group for authentication support");
            }

            // Save customer
            await _customerService.InsertCustomer(customer);
            _logger.LogInformation("Created new guest customer with ID {CustomerId}", customer.Id);
        }
        else
        {
            _logger.LogInformation("Found existing customer with ID {CustomerId}", customer.Id);
        }

        if (model.ShipmentAddress != null)
        {
            _logger.LogInformation("Updating shipping address for customer {CustomerId}", customer.Id);
            var shipAddress = await MapAddressAsync(model.ShipmentAddress);
            shipAddress.AddressType = AddressType.Shipping;
            shipAddress.Email = model.CustomerEmail;

            customer.ShippingAddress = shipAddress;
            await _customerService.UpdateShippingAddress(shipAddress, customer.Id);
        }

        if (model.InvoiceAddress != null)
        {
            _logger.LogInformation("Updating billing address for customer {CustomerId}", customer.Id);
            var billAddress = await MapAddressAsync(model.InvoiceAddress);
            billAddress.AddressType = AddressType.Billing;
            billAddress.Email = model.CustomerEmail;

            customer.BillingAddress = billAddress;
            await _customerService.UpdateBillingAddress(billAddress, customer.Id);
        }
        else if (model.ShipmentAddress != null)
        {
            _logger.LogInformation("Using shipping address as billing for customer {CustomerId}", customer.Id);
            var billAddress = await MapAddressAsync(model.ShipmentAddress);
            billAddress.AddressType = AddressType.Billing;
            billAddress.Email = model.CustomerEmail;

            customer.BillingAddress = billAddress;
            await _customerService.UpdateBillingAddress(billAddress, customer.Id);

            _logger.LogDebug("Set billing address ID {AddressId} for customer {CustomerId}",
                billAddress.Id, customer.Id);
        }

        if (customer.BillingAddress == null)
        {
            _logger.LogError("Customer {CustomerId} has no billing address after processing", customer.Id);
            throw new InvalidOperationException("Billing address is not provided and could not be derived from shipping address");
        }

        return customer;
    }

    public async Task CreateShoppingCartItems(Customer customer, ExternalOrderModel model, IList<Product> products)
    {

        var currentCart = customer.ShoppingCartItems
            .Where(sci => sci.ShoppingCartTypeId == ShoppingCartType.ShoppingCart)
            .ToList();

        if (currentCart != null && currentCart.Any())
        {
            await _customerService.ClearShoppingCartItem(customer.Id, currentCart);
        }

        if (products == null || !products.Any())
        {
            _logger.LogError("No valid products to add to cart for customer {CustomerId}", customer.Id);
            throw new InvalidOperationException("No valid products to add to shopping cart");
        }

        foreach (var line in model.Lines)
        {
            var product = products.FirstOrDefault(p => p.Sku == line.Sku);
            if (product != null)
            {
                int quantity = Math.Max(1, line.Quantity); // Minimum quantity of 1

                var cartItem = new ShoppingCartItem {
                    ProductId = product.Id,
                    WarehouseId = product.WarehouseId,
                    Quantity = quantity,
                    CreatedOnUtc = DateTime.UtcNow,
                    UpdatedOnUtc = DateTime.UtcNow,
                    ShoppingCartTypeId = ShoppingCartType.ShoppingCart,
                    StoreId = _contextAccessor.StoreContext.CurrentStore.Id
                };

                customer.ShoppingCartItems.Add(cartItem);
            }
            else
            {
                _logger.LogError("Product with SKU {Sku} not found in the validated product list", line.Sku);
                throw new InvalidOperationException($"Product with SKU '{line.Sku}' not found in the validated product list");
            }
        }
    }

    public async Task SetPaymentMethod(Customer customer, string paymentMethodSystemName)
    {
        await _customerService.UpdateUserField(customer,
            SystemCustomerFieldNames.SelectedPaymentMethod,
            paymentMethodSystemName,
            _contextAccessor.StoreContext.CurrentStore.Id);
        customer.UserFields.FirstOrDefault(uf => uf.Key == SystemCustomerFieldNames.SelectedPaymentMethod).Value = paymentMethodSystemName;
    }

    public async Task<OrderProcessResult> PlaceOrder(Customer customer)
    {
        var result = new OrderProcessResult();

        var placeOrderResult = await _mediator.Send(new PlaceOrderCommand() {
            Customer = customer
        });

        if (placeOrderResult.Success)
        {
            _logger.LogInformation("Order successfully placed. Order ID: {OrderId}, Order Number: {OrderNumber}",
                placeOrderResult.PlacedOrder.Id, placeOrderResult.PlacedOrder.OrderNumber);

            result.IsSuccess = true;
            result.Order = placeOrderResult.PlacedOrder;
            result.Errors = new List<string>();
            return result;
        }
        else
        {
            _logger.LogError("Order placement failed with errors: {Errors}",
                string.Join(", ", placeOrderResult.Errors));

            result.IsSuccess = false;
            result.Order = null;
            result.Errors = placeOrderResult.Errors;
            return result;
        }
    }


    private async Task<Address> MapAddressAsync(AddressModel model)
    {
        var address = new Address {
            FirstName = model.FirstName ?? string.Empty,
            LastName = model.LastName ?? string.Empty,
            //Email = model.AddressLines?.FirstOrDefault() ?? model.Email ?? string.Empty,
            Company = model.Company,
            Address1 = model.Address1,
            Address2 = model.Address2,
            City = model.City,
            ZipPostalCode = model.PostalCode,
            PhoneNumber = model.Phone
            // CreatedOnUtc property not available in this version of Address
        };

        // Try to resolve country by code
        if (!string.IsNullOrEmpty(model.CountryCode))
        {
            var countries = await _countryService.GetAllCountries();
            var country = countries.FirstOrDefault(c =>
                string.Equals(c.TwoLetterIsoCode, model.CountryCode, StringComparison.InvariantCultureIgnoreCase));

            if (country != null)
            {
                address.CountryId = country.Id;
            }
        }

        return address;
    }

}
